#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Casting.Chromecast.Protocol;
using Screenbox.Casting.Chromecast.Transport;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Models;

namespace Screenbox.Casting.Chromecast;

/// <summary>
/// Cast v2 session controller for Chromecast media receiver.
/// </summary>
public sealed class ChromecastSession : ICastSession
{
    private const string SenderId = "sender-screenbox";
    private const string ReceiverId = "receiver-0";
    private const string DefaultMediaReceiverAppId = "CC1AD845";

    private readonly ChromecastTlsChannel _channel;
    private readonly ICastCompatibilityAnalyzer _compatibilityAnalyzer;

    private int _requestId;
    private string _destinationId = ReceiverId;

    public event EventHandler<CastSessionState>? StateChanged;

    public CastSessionState State { get; private set; } = CastSessionState.Disconnected;

    public CastDevice? Device { get; private set; }

    public ChromecastSession(ICastCompatibilityAnalyzer compatibilityAnalyzer)
    {
        _channel = new ChromecastTlsChannel();
        _compatibilityAnalyzer = compatibilityAnalyzer;
    }

    /// <summary>
    /// Connects to Chromecast endpoint and opens receiver channel.
    /// </summary>
    public async Task ConnectAsync(CastDevice device, CancellationToken cancellationToken = default)
    {
        SetState(CastSessionState.Connecting);

        await _channel.ConnectAsync(device.Host, device.Port, cancellationToken).ConfigureAwait(false);
        Device = device;

        await SendConnectionMessageAsync(ReceiverId, cancellationToken).ConfigureAwait(false);
        await SendReceiverCommandAsync(new Dictionary<string, object>
        {
            ["type"] = "GET_STATUS",
            ["requestId"] = NextRequestId(),
        }, cancellationToken).ConfigureAwait(false);

        SetState(CastSessionState.Connected);
    }

    /// <summary>
    /// Launches default media receiver and updates destination route.
    /// </summary>
    public async Task LaunchDefaultReceiverAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();
        SetState(CastSessionState.ReceiverLaunching);

        await SendReceiverCommandAsync(new Dictionary<string, object>
        {
            ["type"] = "LAUNCH",
            ["appId"] = DefaultMediaReceiverAppId,
            ["requestId"] = NextRequestId(),
        }, cancellationToken).ConfigureAwait(false);

        // Transport id is resolved from RECEIVER_STATUS in full implementation.
        _destinationId = ReceiverId;
        SetState(CastSessionState.ReceiverReady);
    }

    /// <summary>
    /// Loads media while enforcing no-remux/no-transcode policy.
    /// </summary>
    public async Task<CastCompatibilityResult> LoadAsync(CastMediaSource source, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();

        CastCompatibilityResult compatibility = _compatibilityAnalyzer.Analyze(source);
        if (compatibility.Compatibility == CastCompatibility.RequiresRemuxOrTranscode)
        {
            return compatibility;
        }

        Dictionary<string, object?> payload = new()
        {
            ["type"] = "LOAD",
            ["requestId"] = NextRequestId(),
            ["autoplay"] = true,
            ["currentTime"] = 0,
            ["media"] = new Dictionary<string, object?>
            {
                ["contentId"] = source.ContentUri.ToString(),
                ["streamType"] = source.IsLive ? "LIVE" : "BUFFERED",
                ["contentType"] = source.ContentType,
                ["metadata"] = new Dictionary<string, object?>
                {
                    ["metadataType"] = 0,
                    ["title"] = source.Title,
                },
            },
        };

        if (source.PosterUri is not null)
        {
            ((Dictionary<string, object?>)((Dictionary<string, object?>)payload["media"]!)["metadata"]!)["images"] =
            new[] { new Dictionary<string, object?> { ["url"] = source.PosterUri } };
        }

        await SendMediaCommandAsync(payload, cancellationToken).ConfigureAwait(false);
        SetState(CastSessionState.MediaLoaded);
        return compatibility;
    }

    /// <summary>
    /// Sends PLAY for current media session.
    /// </summary>
    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();

        await SendMediaCommandAsync(new Dictionary<string, object>
        {
            ["type"] = "PLAY",
            ["requestId"] = NextRequestId(),
        }, cancellationToken).ConfigureAwait(false);

        SetState(CastSessionState.Playing);
    }

    /// <summary>
    /// Sends PAUSE for current media session.
    /// </summary>
    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();

        await SendMediaCommandAsync(new Dictionary<string, object>
        {
            ["type"] = "PAUSE",
            ["requestId"] = NextRequestId(),
        }, cancellationToken).ConfigureAwait(false);

        SetState(CastSessionState.Paused);
    }

    /// <summary>
    /// Sends STOP for current media session.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();

        await SendMediaCommandAsync(new Dictionary<string, object>
        {
            ["type"] = "STOP",
            ["requestId"] = NextRequestId(),
        }, cancellationToken).ConfigureAwait(false);

        SetState(CastSessionState.Stopped);
    }

    /// <summary>
    /// Closes Cast session and resets state.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_channel.IsConnected)
        {
            await _channel.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        _destinationId = ReceiverId;
        Device = null;
        SetState(CastSessionState.Disconnected);
    }

    /// <summary>
    /// Disposes Cast session resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        await _channel.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a command to receiver namespace.
    /// </summary>
    private Task SendReceiverCommandAsync(object payload, CancellationToken cancellationToken)
    {
        return SendJsonMessageAsync(ReceiverId, CastNamespaces.Receiver, payload, cancellationToken);
    }

    /// <summary>
    /// Sends a command to media namespace.
    /// </summary>
    private Task SendMediaCommandAsync(object payload, CancellationToken cancellationToken)
    {
        return SendJsonMessageAsync(_destinationId, CastNamespaces.Media, payload, cancellationToken);
    }

    /// <summary>
    /// Sends CONNECT to target destination.
    /// </summary>
    private Task SendConnectionMessageAsync(string destinationId, CancellationToken cancellationToken)
    {
        return SendJsonMessageAsync(destinationId, CastNamespaces.Connection, new Dictionary<string, object>
        {
            ["type"] = "CONNECT",
        }, cancellationToken);
    }

    /// <summary>
    /// Builds and sends a UTF-8 JSON Cast channel message.
    /// </summary>
    private Task SendJsonMessageAsync(string destinationId, string messageNamespace, object payload, CancellationToken cancellationToken)
    {
        CastChannelMessage message = new()
        {
            source_id = SenderId,
            destination_id = destinationId,
            @namespace = messageNamespace,
            payload_type = CastChannelMessage.PayloadType.String,
            payload_utf8 = JsonSerializer.Serialize(payload),
        };

        return _channel.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Returns next request identifier.
    /// </summary>
    private int NextRequestId()
    {
        _requestId++;
        return _requestId;
    }

    /// <summary>
    /// Ensures channel is connected before issuing commands.
    /// </summary>
    private void ThrowIfNotConnected()
    {
        if (!_channel.IsConnected || Device is null)
        {
            throw new InvalidOperationException("Chromecast session is not connected.");
        }
    }

    /// <summary>
    /// Applies state transition and notifies listeners.
    /// </summary>
    private void SetState(CastSessionState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, state);
    }
}

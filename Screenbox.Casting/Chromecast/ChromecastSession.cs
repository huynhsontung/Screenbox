#nullable enable

using System;
using System.Collections.Concurrent;
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
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(15);

    private readonly ChromecastTlsChannel _channel;
    private readonly ICastCompatibilityAnalyzer _compatibilityAnalyzer;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<string>> _pendingRequests;

    private int _requestId;
    private int? _mediaSessionId;
    private string _destinationId = ReceiverId;
    private DateTime _lastHeartbeatUtc;
    private CancellationTokenSource? _backgroundLoopCts;
    private Task? _receiveLoopTask;
    private Task? _heartbeatLoopTask;

    public event EventHandler<CastSessionState>? StateChanged;

    public CastSessionState State { get; private set; } = CastSessionState.Disconnected;

    public CastDevice? Device { get; private set; }

    public ChromecastSession(ICastCompatibilityAnalyzer compatibilityAnalyzer)
    {
        _channel = new ChromecastTlsChannel();
        _compatibilityAnalyzer = compatibilityAnalyzer;
        _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<string>>();
    }

    /// <summary>
    /// Connects to Chromecast endpoint and opens receiver channel.
    /// </summary>
    public async Task ConnectAsync(CastDevice device, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        SetState(CastSessionState.Connecting);

        await _channel.ConnectAsync(device.Host, device.Port, cancellationToken).ConfigureAwait(false);
        Device = device;
        _lastHeartbeatUtc = DateTime.UtcNow;

        _backgroundLoopCts = new CancellationTokenSource();
        _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_backgroundLoopCts.Token));
        _heartbeatLoopTask = Task.Run(() => HeartbeatLoopAsync(_backgroundLoopCts.Token));

        await SendConnectionMessageAsync(ReceiverId, cancellationToken).ConfigureAwait(false);
        string receiverStatus = await SendReceiverCommandAsync(new Dictionary<string, object?>
        {
            ["type"] = "GET_STATUS",
        }, cancellationToken).ConfigureAwait(false);
        TryUpdateReceiverRouting(receiverStatus);

        SetState(CastSessionState.Connected);
    }

    /// <summary>
    /// Launches default media receiver and updates destination route.
    /// </summary>
    public async Task LaunchDefaultReceiverAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();
        SetState(CastSessionState.ReceiverLaunching);

        string receiverStatus = await SendReceiverCommandAsync(new Dictionary<string, object?>
        {
            ["type"] = "LAUNCH",
            ["appId"] = DefaultMediaReceiverAppId,
        }, cancellationToken).ConfigureAwait(false);

        TryUpdateReceiverRouting(receiverStatus);
        await SendConnectionMessageAsync(_destinationId, cancellationToken).ConfigureAwait(false);

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

        string mediaStatus = await SendMediaCommandAsync(payload, cancellationToken).ConfigureAwait(false);
        TryUpdateMediaStatus(mediaStatus);
        SetState(CastSessionState.MediaLoaded);
        return compatibility;
    }

    /// <summary>
    /// Sends PLAY for current media session.
    /// </summary>
    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();

        string mediaStatus = await SendMediaCommandAsync(new Dictionary<string, object?>
        {
            ["type"] = "PLAY",
            ["mediaSessionId"] = _mediaSessionId,
        }, cancellationToken).ConfigureAwait(false);

        TryUpdateMediaStatus(mediaStatus);
    }

    /// <summary>
    /// Sends PAUSE for current media session.
    /// </summary>
    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();

        string mediaStatus = await SendMediaCommandAsync(new Dictionary<string, object?>
        {
            ["type"] = "PAUSE",
            ["mediaSessionId"] = _mediaSessionId,
        }, cancellationToken).ConfigureAwait(false);

        TryUpdateMediaStatus(mediaStatus);
    }

    /// <summary>
    /// Sends STOP for current media session.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConnected();

        string mediaStatus = await SendMediaCommandAsync(new Dictionary<string, object?>
        {
            ["type"] = "STOP",
            ["mediaSessionId"] = _mediaSessionId,
        }, cancellationToken).ConfigureAwait(false);

        TryUpdateMediaStatus(mediaStatus);
    }

    /// <summary>
    /// Closes Cast session and resets state.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_backgroundLoopCts is not null)
        {
            _backgroundLoopCts.Cancel();
        }

        if (_receiveLoopTask is not null)
        {
            await AwaitBackgroundTaskAsync(_receiveLoopTask).ConfigureAwait(false);
            _receiveLoopTask = null;
        }

        if (_heartbeatLoopTask is not null)
        {
            await AwaitBackgroundTaskAsync(_heartbeatLoopTask).ConfigureAwait(false);
            _heartbeatLoopTask = null;
        }

        _backgroundLoopCts?.Dispose();
        _backgroundLoopCts = null;

        if (_channel.IsConnected)
        {
            await _channel.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (KeyValuePair<int, TaskCompletionSource<string>> pending in _pendingRequests)
        {
            pending.Value.TrySetCanceled();
        }

        _pendingRequests.Clear();

        _destinationId = ReceiverId;
        _mediaSessionId = null;
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
    /// Receives Cast channel messages and processes routing and request completion.
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _channel.IsConnected)
        {
            try
            {
                CastChannelMessage message = await _channel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                _lastHeartbeatUtc = DateTime.UtcNow;

                if (message.@namespace == CastNamespaces.Heartbeat)
                {
                    await HandleHeartbeatAsync(message, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (message.payload_type != CastChannelMessage.PayloadType.String || string.IsNullOrWhiteSpace(message.payload_utf8))
                {
                    continue;
                }

                string payload = message.payload_utf8!;
                ProcessResponsePayload(payload);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                SetState(CastSessionState.Faulted);
                return;
            }
        }
    }

    /// <summary>
    /// Sends periodic heartbeats and marks session faulted on timeout.
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _channel.IsConnected)
        {
            try
            {
                await Task.Delay(HeartbeatInterval, cancellationToken).ConfigureAwait(false);

                if (DateTime.UtcNow - _lastHeartbeatUtc > HeartbeatTimeout)
                {
                    SetState(CastSessionState.Faulted);
                    await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                await SendJsonMessageAsync(
                    ReceiverId,
                    CastNamespaces.Heartbeat,
                    new Dictionary<string, object?>
                    {
                        ["type"] = "PING",
                    },
                    requestId: null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                SetState(CastSessionState.Faulted);
                return;
            }
        }
    }

    /// <summary>
    /// Handles heartbeat namespace requests from Chromecast.
    /// </summary>
    private Task HandleHeartbeatAsync(CastChannelMessage message, CancellationToken cancellationToken)
    {
        if (!string.Equals(message.payload_utf8, "{\"type\":\"PING\"}", StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        return SendJsonMessageAsync(
            message.source_id,
            CastNamespaces.Heartbeat,
            new Dictionary<string, object?>
            {
                ["type"] = "PONG",
            },
            requestId: null,
            cancellationToken);
    }

    /// <summary>
    /// Sends a command to receiver namespace and awaits a correlated response.
    /// </summary>
    private Task<string> SendReceiverCommandAsync(Dictionary<string, object?> payload, CancellationToken cancellationToken)
    {
        return SendCommandAsync(ReceiverId, CastNamespaces.Receiver, payload, cancellationToken);
    }

    /// <summary>
    /// Sends a command to media namespace and awaits a correlated response.
    /// </summary>
    private Task<string> SendMediaCommandAsync(Dictionary<string, object?> payload, CancellationToken cancellationToken)
    {
        return SendCommandAsync(_destinationId, CastNamespaces.Media, payload, cancellationToken);
    }

    /// <summary>
    /// Sends CONNECT command to target destination.
    /// </summary>
    private Task SendConnectionMessageAsync(string destinationId, CancellationToken cancellationToken)
    {
        return SendJsonMessageAsync(destinationId, CastNamespaces.Connection, new Dictionary<string, object?>
        {
            ["type"] = "CONNECT",
        }, requestId: null, cancellationToken);
    }

    /// <summary>
    /// Sends a command and waits for a response carrying the same request identifier.
    /// </summary>
    private async Task<string> SendCommandAsync(string destinationId, string messageNamespace, Dictionary<string, object?> payload, CancellationToken cancellationToken)
    {
        int requestId = NextRequestId();
        payload["requestId"] = requestId;

        TaskCompletionSource<string> responseCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(requestId, responseCompletion))
        {
            throw new InvalidOperationException($"Duplicate Cast request id {requestId}.");
        }

        await SendJsonMessageAsync(destinationId, messageNamespace, payload, requestId, cancellationToken).ConfigureAwait(false);

        Task completed = await Task.WhenAny(responseCompletion.Task, Task.Delay(CommandTimeout, cancellationToken)).ConfigureAwait(false);
        if (completed != responseCompletion.Task)
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw new TimeoutException($"Timed out waiting for Cast response for request {requestId}.");
        }

        return await responseCompletion.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Builds and sends a UTF-8 JSON Cast channel message.
    /// </summary>
    private Task SendJsonMessageAsync(string destinationId, string messageNamespace, object payload, int? requestId, CancellationToken cancellationToken)
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
    /// Processes response JSON payload for request completion and state updates.
    /// </summary>
    private void ProcessResponsePayload(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;

        if (TryGetInt32(root, "requestId", out int requestId) && _pendingRequests.TryRemove(requestId, out TaskCompletionSource<string>? completion))
        {
            completion.TrySetResult(payload);
        }

        if (!TryGetString(root, "type", out string? messageType))
        {
            return;
        }

        if (string.Equals(messageType, "RECEIVER_STATUS", StringComparison.OrdinalIgnoreCase))
        {
            TryUpdateReceiverRouting(payload);
            return;
        }

        if (string.Equals(messageType, "MEDIA_STATUS", StringComparison.OrdinalIgnoreCase))
        {
            TryUpdateMediaStatus(payload);
        }
    }

    /// <summary>
    /// Updates current transport destination from receiver status payload.
    /// </summary>
    private void TryUpdateReceiverRouting(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("status", out JsonElement status))
        {
            return;
        }

        if (!status.TryGetProperty("applications", out JsonElement applications) || applications.ValueKind != JsonValueKind.Array)
        {
            _destinationId = ReceiverId;
            return;
        }

        foreach (JsonElement application in applications.EnumerateArray())
        {
            if (!TryGetString(application, "appId", out string? appId) || !string.Equals(appId, DefaultMediaReceiverAppId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryGetString(application, "transportId", out string? transportId) && !string.IsNullOrWhiteSpace(transportId))
            {
                _destinationId = transportId!;
                return;
            }
        }
    }

    /// <summary>
    /// Updates media session identifier and playback state from media status payload.
    /// </summary>
    private void TryUpdateMediaStatus(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("status", out JsonElement statusArray) || statusArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        JsonElement status = default;
        bool hasStatus = false;
        foreach (JsonElement item in statusArray.EnumerateArray())
        {
            status = item;
            hasStatus = true;
            break;
        }

        if (!hasStatus || status.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        if (TryGetInt32(status, "mediaSessionId", out int mediaSessionId))
        {
            _mediaSessionId = mediaSessionId;
        }

        if (!TryGetString(status, "playerState", out string? playerState))
        {
            return;
        }

        switch (playerState)
        {
            case "PLAYING":
                SetState(CastSessionState.Playing);
                break;
            case "PAUSED":
                SetState(CastSessionState.Paused);
                break;
            case "IDLE":
                SetState(CastSessionState.Stopped);
                break;
        }
    }

    /// <summary>
    /// Safely awaits a background task and swallows cancellation/fault propagation.
    /// </summary>
    private static async Task AwaitBackgroundTaskAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // Background loop failures are reflected through session state transitions.
        }
    }

    /// <summary>
    /// Attempts to read a string property from JSON object.
    /// </summary>
    private static bool TryGetString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    /// <summary>
    /// Attempts to read an Int32 property from JSON object.
    /// </summary>
    private static bool TryGetInt32(JsonElement root, string name, out int value)
    {
        value = 0;
        if (!root.TryGetProperty(name, out JsonElement property) || property.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        return property.TryGetInt32(out value);
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

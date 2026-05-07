#nullable enable

using System;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Sharpcaster;
using Sharpcaster.Models.ChromecastStatus;
using Sharpcaster.Models.Media;
using Windows.System;

namespace Screenbox.Casting.Chromecast;

/// <summary>
/// An active cast session backed by SharpCaster's <see cref="ChromecastClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// State properties (<see cref="Position"/>, <see cref="IsPlaying"/>, etc.) are updated
/// by subscribing to <see cref="Sharpcaster.Channels.MediaChannel.StatusChanged"/> and
/// <see cref="Sharpcaster.Channels.ReceiverChannel.ReceiverStatusChanged"/>.
/// </para>
/// <para>
/// <see cref="PlaybackEnded"/> is raised when the device transitions to the
/// <c>IDLE</c> state with reason <c>FINISHED</c>, <c>ERROR</c>, or <c>CANCELLED</c>.
/// </para>
/// <para>
/// Use the static <see cref="CreateAsync"/> factory to create instances.
/// </para>
/// </remarks>
public sealed class ChromecastSession : ICastSession
{
    /// <inheritdoc/>
    public ICastDevice Device { get; }

    /// <inheritdoc/>
    public double Position { get; private set; }

    /// <inheritdoc/>
    public double Duration { get; private set; }

    /// <inheritdoc/>
    public bool IsPlaying { get; private set; }

    /// <inheritdoc/>
    public bool IsBuffering { get; private set; }

    /// <inheritdoc/>
    public double Volume { get; private set; }

    /// <inheritdoc/>
    public bool IsMuted { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? PlaybackEnded;

    /// <inheritdoc/>
    public event EventHandler? Disconnected;

    private readonly ChromecastClient _client;
    private readonly DispatcherQueue _dispatcherQueue;
    private bool _disposed;

    private ChromecastSession(ChromecastDevice device, ChromecastClient client)
    {
        Device = device;
        _client = client;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _client.MediaChannel.StatusChanged += OnMediaStatusChanged;
        _client.ReceiverChannel.ReceiverStatusChanged += OnReceiverStatusChanged;
        _client.Disconnected += OnClientDisconnected;
    }

    /// <summary>
    /// Connects to the Chromecast device, launches the Default Media Receiver application,
    /// loads <paramref name="streamUrl"/>, and optionally seeks to <paramref name="startPosition"/>.
    /// </summary>
    /// <param name="device">The target Chromecast device.</param>
    /// <param name="streamUrl">The HTTP URL the Chromecast should fetch and play.</param>
    /// <param name="startPosition">Seek position after loading; ignored when zero.</param>
    /// <param name="applicationId">The Cast application ID to launch (e.g., <c>CC1AD845</c>).</param>
    /// <returns>
    /// A connected <see cref="ChromecastSession"/> on success; <c>null</c> if the connection fails.
    /// </returns>
    public static async Task<ChromecastSession?> CreateAsync(
        ChromecastDevice device,
        Uri streamUrl,
        TimeSpan startPosition,
        string applicationId)
    {
        ChromecastClient? client = null;

        try
        {
            client = new ChromecastClient();
            await client.ConnectChromecast(device.Receiver).ConfigureAwait(false);
            await client.LaunchApplicationAsync(applicationId).ConfigureAwait(false);

            var media = new Media
            {
                ContentUrl = streamUrl.ToString(),
                ContentType = InferContentType(streamUrl),
                // BUFFERED allows seeking; LIVE disables it.
                StreamType = StreamType.Buffered,
            };

            await client.MediaChannel.LoadAsync(media, autoPlay: true).ConfigureAwait(false);

            if (startPosition > TimeSpan.Zero)
            {
                await client.MediaChannel.SeekAsync(startPosition.TotalSeconds).ConfigureAwait(false);
            }

            return new ChromecastSession(device, client);
        }
        catch (Exception)
        {
            // Clean up a partially-started session so the device is not left in a broken state.
            if (client is not null)
            {
                try { await client.MediaChannel.StopAsync().ConfigureAwait(false); } catch { }
                try { await client.DisconnectAsync().ConfigureAwait(false); } catch { }
            }

            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Playback controls
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task PlayAsync()
    {
        try
        {
            await _client.MediaChannel.PlayAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore — the device may be disconnecting or the session may have ended.
        }
    }

    /// <inheritdoc/>
    public async Task PauseAsync()
    {
        try
        {
            await _client.MediaChannel.PauseAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore — the device may be disconnecting or the session may have ended.
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        try
        {
            await _client.MediaChannel.StopAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore — the device may be disconnecting or the session may have ended.
        }
    }

    /// <inheritdoc/>
    public async Task SeekAsync(TimeSpan position)
    {
        try
        {
            await _client.MediaChannel.SeekAsync(position.TotalSeconds).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore — the device may be disconnecting or the session may have ended.
        }
    }

    /// <inheritdoc/>
    public async Task SetVolumeAsync(double level)
    {
        try
        {
            await _client.ReceiverChannel.SetVolume(Math.Clamp(level, 0.0, 1.0)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore — the device may be disconnecting or the session may have ended.
        }
    }

    /// <inheritdoc/>
    public async Task SetMuteAsync(bool muted)
    {
        try
        {
            await _client.ReceiverChannel.SetMute(muted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore — the device may be disconnecting or the session may have ended.
        }
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _client.MediaChannel.StatusChanged -= OnMediaStatusChanged;
        _client.ReceiverChannel.ReceiverStatusChanged -= OnReceiverStatusChanged;
        _client.Disconnected -= OnClientDisconnected;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Handles status pushes from the Chromecast media channel.
    /// Updates state properties and raises <see cref="PlaybackEnded"/> on natural playback end.
    /// </summary>
    private void OnMediaStatusChanged(object sender, MediaStatus status)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Position = status.CurrentTime;

            // Duration is nullable; only update when a positive value is reported.
            if (status.Media?.Duration is { } duration && duration > 0)
            {
                Duration = duration;
            }

            IsPlaying = status.PlayerState is PlayerStateType.Playing;
            IsBuffering = status.PlayerState is PlayerStateType.Buffering;

            // IdleReason is a plain string in SharpCaster 3.x.
            // Values defined by the Cast protocol: FINISHED, CANCELLED, ERROR, INTERRUPTED.
            if (status.PlayerState is PlayerStateType.Idle &&
                status.IdleReason is "FINISHED" or "ERROR" or "CANCELLED")
            {
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    /// <summary>
    /// Handles receiver status pushes from the Chromecast device (volume / mute state).
    /// </summary>
    private void OnReceiverStatusChanged(object sender, ChromecastStatus status)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (status.Volume?.Level is { } level)
            {
                Volume = level;
            }

            if (status.Volume?.Muted is { } muted)
            {
                IsMuted = muted;
            }
        });
    }

    /// <summary>
    /// Handles an unexpected disconnection from the Chromecast device.
    /// </summary>
    private void OnClientDisconnected(object sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => Disconnected?.Invoke(this, EventArgs.Empty));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Infers a MIME content-type from the URL's file extension.
    /// Falls back to <c>video/mp4</c> for unknown extensions.
    /// </summary>
    private static string InferContentType(Uri url)
    {
        string path = url.AbsolutePath;
        int dotIndex = path.LastIndexOf('.');
        string ext = dotIndex >= 0 ? path.Substring(dotIndex).ToLowerInvariant() : string.Empty;

        return ext switch
        {
            ".mp4" or ".m4v" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".mp3" => "audio/mpeg",
            ".m4a" or ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".wav" => "audio/wav",
            ".ogg" or ".opus" => "audio/ogg",
            _ => "video/mp4",
        };
    }
}

#nullable enable

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Windows.System;

namespace Screenbox.Casting.Dlna;

/// <summary>
/// An active cast session backed by UPnP AVTransport and RenderingControl SOAP commands.
/// </summary>
/// <remarks>
/// <para>
/// DLNA has no push-notification model, so a 1 s polling timer drives state updates.
/// The timer calls <c>GetPositionInfo</c> and <c>GetTransportInfo</c> and updates the
/// public state properties.  When the transport transitions to <c>STOPPED</c> after having
/// been in a playing or paused state, <see cref="PlaybackEnded"/> is raised.
/// </para>
/// <para>
/// Use the static <see cref="CreateAsync"/> factory to create instances.
/// </para>
/// </remarks>
public sealed class DlnaSession : ICastSession
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    // Transport states reported by UPnP GetTransportInfo.
    private const string StateIdle = "NO_MEDIA_PRESENT";
    private const string StateStopped = "STOPPED";
    private const string StatePlaying = "PLAYING";
    private const string StatePausedPlayback = "PAUSED_PLAYBACK";
    private const string StateTransitioning = "TRANSITIONING";

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

    private readonly DlnaAvTransportClient _avTransport;
    private readonly DlnaRenderingControlClient _renderingControl;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Timer _pollTimer;
    private bool _playbackStarted;
    private bool _disposed;

    private DlnaSession(DlnaDevice device, DlnaAvTransportClient avTransport, DlnaRenderingControlClient renderingControl)
    {
        Device = device;
        _avTransport = avTransport;
        _renderingControl = renderingControl;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _pollTimer = new Timer(OnPollTimer, null, PollInterval, PollInterval);
    }

    /// <summary>
    /// Sends <c>SetAVTransportURI</c> and <c>Play</c> to start playback on the renderer,
    /// then seeks to <paramref name="startPosition"/> if it is greater than zero.
    /// </summary>
    /// <param name="device">The target DLNA device.</param>
    /// <param name="streamUrl">The HTTP URL of the media stream.</param>
    /// <param name="startPosition">Seek position after starting; ignored when zero.</param>
    /// <returns>
    /// A <see cref="DlnaSession"/> on success; <c>null</c> if the initial SOAP commands fail.
    /// </returns>
    public static async Task<DlnaSession?> CreateAsync(DlnaDevice device, Uri streamUrl, TimeSpan startPosition)
    {
        HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        DlnaAvTransportClient avTransport = new(device.AvTransportControlUrl, httpClient);
        DlnaRenderingControlClient renderingControl = new(device.RenderingControlUrl, httpClient);

        try
        {
            await avTransport.SetAvTransportUriAsync(streamUrl.ToString()).ConfigureAwait(false);
            await avTransport.PlayAsync().ConfigureAwait(false);

            if (startPosition > TimeSpan.Zero)
            {
                await avTransport.SeekAsync(startPosition).ConfigureAwait(false);
            }

            return new DlnaSession(device, avTransport, renderingControl);
        }
        catch (Exception)
        {
            httpClient.Dispose();
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Playback controls
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task PlayAsync()
    {
        try { await _avTransport.PlayAsync().ConfigureAwait(false); }
        catch (Exception) { }
    }

    /// <inheritdoc/>
    public async Task PauseAsync()
    {
        try { await _avTransport.PauseAsync().ConfigureAwait(false); }
        catch (Exception) { }
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        try { await _avTransport.StopAsync().ConfigureAwait(false); }
        catch (Exception) { }
    }

    /// <inheritdoc/>
    public async Task SeekAsync(TimeSpan position)
    {
        try { await _avTransport.SeekAsync(position).ConfigureAwait(false); }
        catch (Exception) { }
    }

    /// <inheritdoc/>
    public async Task SetVolumeAsync(double level)
    {
        try { await _renderingControl.SetVolumeAsync(level).ConfigureAwait(false); }
        catch (Exception) { }
    }

    /// <inheritdoc/>
    public async Task SetMuteAsync(bool muted)
    {
        try { await _renderingControl.SetMuteAsync(muted).ConfigureAwait(false); }
        catch (Exception) { }
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pollTimer.Dispose();
    }

    // -------------------------------------------------------------------------
    // Polling
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by <see cref="_pollTimer"/> every <see cref="PollInterval"/>.
    /// Queries position and transport state, updates observable properties,
    /// and raises <see cref="PlaybackEnded"/> when appropriate.
    /// </summary>
    private async void OnPollTimer(object? state)
    {
        if (_disposed) return;

        try
        {
            // Query position and transport state in parallel.
            Task<(double, double)?> posTask = _avTransport.GetPositionInfoAsync();
            Task<string?> stateTask = _avTransport.GetTransportInfoAsync();

            await Task.WhenAll(posTask, stateTask).ConfigureAwait(false);

            var posResult = posTask.Result;
            string? transportState = stateTask.Result;

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (posResult is { } pos)
                {
                    var (positionSecs, durationSecs) = pos;
                    Position = positionSecs;
                    if (durationSecs > 0) Duration = durationSecs;
                }

                if (transportState is not null)
                {
                    bool wasStarted = _playbackStarted;

                    IsPlaying = transportState == StatePlaying;
                    IsBuffering = transportState == StateTransitioning;

                    // Mark that playback has actually started so we can detect when it ends.
                    if (IsPlaying) _playbackStarted = true;

                    // Raise PlaybackEnded when the device transitions to STOPPED after playing.
                    if (wasStarted && transportState is StateStopped or StateIdle)
                    {
                        _playbackStarted = false;
                        PlaybackEnded?.Invoke(this, EventArgs.Empty);
                    }
                }
            });
        }
        catch (Exception)
        {
            // Network failure — raise Disconnected so the app can react.
            _dispatcherQueue.TryEnqueue(() => Disconnected?.Invoke(this, EventArgs.Empty));
            Dispose();
        }
    }
}

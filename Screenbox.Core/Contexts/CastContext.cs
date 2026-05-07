#nullable enable

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Casting.Abstractions;
using Windows.System;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Shared singleton context that carries the current cast session state.
/// Observable properties are polled from the active <see cref="ICastSession"/> on a timer
/// and propagated to any view model that depends on this context.
/// </summary>
public sealed partial class CastContext : ObservableObject
{
    /// <summary>The active cast session, or <c>null</c> when not casting.</summary>
    [ObservableProperty]
    private ICastSession? _session;

    /// <summary>
    /// <c>true</c> while a cast session is active; <c>false</c> otherwise.
    /// </summary>
    [ObservableProperty]
    private bool _isCasting;

    /// <summary>Current playback position reported by the cast device, in seconds.</summary>
    [ObservableProperty]
    private double _castPosition;

    /// <summary>
    /// Duration of the currently-loaded media on the cast device, in seconds.
    /// Remains zero until the device reports a valid duration.
    /// </summary>
    [ObservableProperty]
    private double _castDuration;

    /// <summary>
    /// <c>true</c> when the cast device is actively playing (not paused, buffering, or idle).
    /// </summary>
    [ObservableProperty]
    private bool _castIsPlaying;

    /// <summary><c>true</c> when the cast device is buffering media.</summary>
    [ObservableProperty]
    private bool _castIsBuffering;

    /// <summary>
    /// The current volume level reported by the cast device, between 0.0 (silent) and 1.0 (full).
    /// </summary>
    [ObservableProperty]
    private double _castVolume;

    /// <summary><c>true</c> when the cast device is muted.</summary>
    [ObservableProperty]
    private bool _castIsMuted;

    /// <summary>
    /// Raised when the cast device naturally finishes playback or is unexpectedly disconnected,
    /// indicating that the cast session should be cleaned up.
    /// Always raised on the UI thread.
    /// </summary>
    public event EventHandler? CastingNaturallyEnded;

    private readonly DispatcherQueue _dispatcherQueue;
    private DispatcherQueueTimer? _pollTimer;

    public CastContext()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    /// <summary>
    /// Generated partial method called whenever <see cref="Session"/> changes.
    /// Subscribes / unsubscribes session events and starts / stops the polling timer.
    /// </summary>
    partial void OnSessionChanged(ICastSession? oldValue, ICastSession? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PlaybackEnded -= OnSessionPlaybackEnded;
            oldValue.Disconnected -= OnSessionDisconnected;
        }

        if (_pollTimer is not null)
        {
            _pollTimer.Stop();
            _pollTimer = null;
        }

        if (newValue is not null)
        {
            newValue.PlaybackEnded += OnSessionPlaybackEnded;
            newValue.Disconnected += OnSessionDisconnected;

            // Poll session properties every 500 ms so observable properties stay in sync.
            // Both Chromecast and DLNA sessions already update their own properties on the UI thread,
            // so reading them here (also on the UI thread) is safe.
            _pollTimer = _dispatcherQueue.CreateTimer();
            _pollTimer.Interval = TimeSpan.FromMilliseconds(500);
            _pollTimer.IsRepeating = true;
            _pollTimer.Tick += OnPollTimerTick;
            _pollTimer.Start();
        }
    }

    /// <summary>
    /// Polls the session's current state and propagates any changed values to the
    /// observable properties so that subscribed view models are notified.
    /// </summary>
    private void OnPollTimerTick(DispatcherQueueTimer timer, object? args)
    {
        ICastSession? session = _session;
        if (session is null) return;

        CastPosition = session.Position;
        CastIsPlaying = session.IsPlaying;
        CastIsBuffering = session.IsBuffering;
        CastVolume = session.Volume;
        CastIsMuted = session.IsMuted;

        if (session.Duration > 0)
        {
            CastDuration = session.Duration;
        }
    }

    /// <summary>Handles natural playback end reported by the cast session.</summary>
    private void OnSessionPlaybackEnded(object sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => CastingNaturallyEnded?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Handles unexpected disconnection from the cast device.</summary>
    private void OnSessionDisconnected(object sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => CastingNaturallyEnded?.Invoke(this, EventArgs.Empty));
    }
}


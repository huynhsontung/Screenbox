#nullable enable

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Sharpcaster;
using Sharpcaster.Models.ChromecastStatus;
using Sharpcaster.Models.Media;
using Windows.System;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Shared singleton context that carries the current Chromecast cast session state.
/// Observable properties are updated from <see cref="ChromecastClient.MediaChannel"/> status events
/// and propagated to any view model that depends on this context.
/// </summary>
public sealed partial class CastContext : ObservableObject
{
    [ObservableProperty]
    private RendererWatcher? _rendererWatcher;

    [ObservableProperty]
    private Renderer? _activeRenderer;

    /// <summary>The active SharpCaster client for the current cast session, or <c>null</c> when not casting.</summary>
    [ObservableProperty]
    private ChromecastClient? _client;

    /// <summary>
    /// <c>true</c> while a Chromecast cast session is active; <c>false</c> otherwise.
    /// Set to <c>true</c> by <c>CastControlViewModel</c> when a session starts and
    /// back to <c>false</c> when it ends or is stopped.
    /// </summary>
    [ObservableProperty]
    private bool _isCasting;

    /// <summary>Current playback position reported by the Chromecast device, in seconds.</summary>
    [ObservableProperty]
    private double _castPosition;

    /// <summary>
    /// Duration of the currently-loaded media on the Chromecast device, in seconds.
    /// Remains zero until the receiver reports a valid duration.
    /// </summary>
    [ObservableProperty]
    private double _castDuration;

    /// <summary>
    /// <c>true</c> when the Chromecast device is actively playing (not paused, buffering, or idle).
    /// </summary>
    [ObservableProperty]
    private bool _castIsPlaying;

    /// <summary><c>true</c> when the Chromecast device is buffering media.</summary>
    [ObservableProperty]
    private bool _castIsBuffering;

    /// <summary>
    /// The current receiver volume level reported by the Chromecast device, as a value
    /// between 0.0 (silent) and 1.0 (full volume).
    /// Updated from <see cref="Sharpcaster.Channels.ReceiverChannel.ReceiverStatusChanged"/>.
    /// </summary>
    [ObservableProperty]
    private double _castVolume;

    /// <summary>
    /// <c>true</c> when the Chromecast device's receiver is muted.
    /// Updated from <see cref="Sharpcaster.Channels.ReceiverChannel.ReceiverStatusChanged"/>.
    /// </summary>
    [ObservableProperty]
    private bool _castIsMuted;

    /// <summary>
    /// Raised when the Chromecast device transitions to the <c>IDLE</c> state with reason
    /// <c>FINISHED</c>, <c>ERROR</c>, or <c>CANCELLED</c>, indicating that playback ended
    /// naturally and the cast session should be cleaned up.
    /// The event is always raised on the UI thread.
    /// </summary>
    public event EventHandler? CastingNaturallyEnded;

    // The dispatcher captured at construction time is always the main UI thread dispatcher
    // because DI resolves singletons on the UI thread.
    private readonly DispatcherQueue _dispatcherQueue;

    public CastContext()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    /// <summary>
    /// Generated partial method called whenever <see cref="Client"/> changes.
    /// Attaches and detaches the <see cref="ChromecastClient.MediaChannel"/> status-changed
    /// event handler so cast playback state is kept in sync.
    /// </summary>
    partial void OnClientChanged(ChromecastClient? oldValue, ChromecastClient? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.MediaChannel.StatusChanged -= OnMediaStatusChanged;
            oldValue.ReceiverChannel.ReceiverStatusChanged -= OnReceiverStatusChanged;
        }

        if (newValue is not null)
        {
            newValue.MediaChannel.StatusChanged += OnMediaStatusChanged;
            newValue.ReceiverChannel.ReceiverStatusChanged += OnReceiverStatusChanged;
        }
    }

    /// <summary>
    /// Handles status updates pushed by the active Chromecast media channel.
    /// Marshals all property updates onto the UI thread and raises
    /// <see cref="CastingNaturallyEnded"/> when the receiver transitions to IDLE.
    /// </summary>
    private void OnMediaStatusChanged(object sender, MediaStatus status)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            CastPosition = status.CurrentTime;

            // Duration is nullable; only update when a positive value is provided.
            if (status.Media?.Duration is { } duration && duration > 0)
            {
                CastDuration = duration;
            }

            CastIsPlaying = status.PlayerState is PlayerStateType.Playing;
            CastIsBuffering = status.PlayerState is PlayerStateType.Buffering;

            // Detect natural playback end. IdleReason is a string in SharpCaster 3.x.
            // Values are defined by the Google Cast protocol: FINISHED, CANCELLED, ERROR, INTERRUPTED.
            if (status.PlayerState is PlayerStateType.Idle &&
                status.IdleReason is "FINISHED" or "ERROR" or "CANCELLED")
            {
                CastingNaturallyEnded?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    /// <summary>
    /// Handles receiver status updates pushed by the active Chromecast device.
    /// Marshals volume and mute state updates onto the UI thread.
    /// </summary>
    private void OnReceiverStatusChanged(object sender, ChromecastStatus status)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (status.Volume?.Level is { } level)
            {
                CastVolume = level;
            }

            if (status.Volume?.Muted is { } muted)
            {
                CastIsMuted = muted;
            }
        });
    }
}

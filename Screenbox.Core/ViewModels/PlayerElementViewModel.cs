using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Input;


namespace Screenbox.Core.ViewModels;

public sealed partial class PlayerElementViewModel : ObservableRecipient,
    IRecipient<ChangeAspectRatioMessage>
{
    private const double GestureStepAmount = 5.0;

    private enum SlideOrientation
    {
        None,
        Horizontal,
        Vertical
    }

    [ObservableProperty] public partial bool IsHolding { get; set; }

    public event EventHandler<EventArgs>? ClearViewRequested;

    public LibVLCSharp.Shared.MediaPlayer? VlcPlayer => (_playerContext.MediaPlayer as VlcMediaPlayer)?.VlcPlayer;

    private IMediaPlayer? MediaPlayer
    {
        get => _playerContext.MediaPlayer;
        set => _playerContext.MediaPlayer = value;
    }

    private readonly PlayerContext _playerContext;
    private readonly IPlayerService _playerService;
    private readonly ISystemMediaTransportControlsService _transportControlsService;
    private readonly PlayerSettingsContext _playerSettings;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _clickTimer;
    private readonly DispatcherQueueTimer _pointerWheelTimer;
    private readonly DisplayRequestTracker _requestTracker;
    private readonly ILogger<PlayerElementViewModel> _logger;
    private CancellationTokenSource? _initCts;
    private Size _viewSize;
    private Size _aspectRatio;
    private TimeSpan _timeBeforeManipulation;
    private double _playbackRateBeforeHold;
    private SlideOrientation _slideOrientation;
    private double _rawPixelsPerViewPixel = 1.0;

    public PlayerElementViewModel(
        PlayerContext playerContext,
        IPlayerService playerService,
        PlayerSettingsContext playerSettings,
        ISystemMediaTransportControlsService transportControlsService,
        ILogger<PlayerElementViewModel> logger)
    {
        _playerContext = playerContext;
        _playerService = playerService;
        _playerSettings = playerSettings;
        _transportControlsService = transportControlsService;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _clickTimer = _dispatcherQueue.CreateTimer();
        _pointerWheelTimer = _dispatcherQueue.CreateTimer();
        _requestTracker = new DisplayRequestTracker();

        transportControlsService.TransportControls.ButtonPressed += TransportControlsOnButtonPressed;
        transportControlsService.TransportControls.PlaybackPositionChangeRequested += TransportControlsOnPlaybackPositionChangeRequested;

        Messenger.Register<ChangeAspectRatioMessage>(this);
    }

    public void Receive(ChangeAspectRatioMessage message)
    {
        _aspectRatio = message.Value;
        SetCropGeometry(message.Value);
    }

    public void Initialize(string[] swapChainOptions)
    {
        // Unsubscribe events and set MediaPlayer to null immediately so that
        // downstream consumers (e.g. MediaListViewModel._delayPlay) know the player
        // is unavailable and queue any incoming PlayFilesMessage for later processing.
        IMediaPlayer? oldPlayer = MediaPlayer;
        if (oldPlayer != null)
        {
            oldPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
            oldPlayer.PositionChanged -= OnPositionChanged;
            oldPlayer.MediaFailed -= OnMediaFailed;
            oldPlayer.PlaybackItemChanged -= OnPlaybackItemChanged;
            MediaPlayer = null;
        }

        // Cancel any in-progress initialization so that a stale Task cannot
        // overwrite the new player after it becomes ready.
        CancellationTokenSource? oldCts = _initCts;
        _initCts = new CancellationTokenSource();
        CancellationToken ct = _initCts.Token;
        oldCts?.Cancel();
        oldCts?.Dispose();

        // Run both the old-player disposal and new-player initialization on a
        // background thread.  LibVLC teardown (LibVLC.Dispose) is expensive and
        // must not block the UI thread, which would delay window messaging and
        // prevent the app from processing the next activation in time.
        Task.Run(() =>
        {
            // Always dispose the old player even if a newer init was requested —
            // we captured oldPlayer before the cancellation token was reset, so
            // only this invocation owns the reference.
            if (oldPlayer != null)
            {
                _playerService.DisposePlayer(oldPlayer);
            }

            if (ct.IsCancellationRequested) return;

            try
            {
                var args = new List<string>();
                if (_playerSettings.GlobalArguments.Length > 0)
                {
                    args.AddRange(_playerSettings.GlobalArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }

                if (_playerSettings.VideoUpscale != VideoUpscaleOption.Linear)
                {
                    args.Add($"--d3d11-upscale-mode={_playerSettings.VideoUpscale.ToString().ToLower()}");
                }

                args.AddRange(swapChainOptions);
                IMediaPlayer player;
                ExceptionDispatchInfo? initException = null;
                try
                {
                    player = _playerService.Initialize(args.ToArray());
                }
                catch (VLCException e)
                {
                    player = _playerService.Initialize(swapChainOptions);
                    initException = ExceptionDispatchInfo.Capture(e);  // Passable exception
                }

                if (player is not VlcMediaPlayer vlcMediaPlayer)
                {
                    throw new InvalidOperationException("PlayerService must return a VlcMediaPlayer instance.");
                }

                if (ct.IsCancellationRequested)
                {
                    // A newer Initialize() call won the race; discard this player to avoid a leak.
                    _playerService.DisposePlayer(vlcMediaPlayer);
                    return;
                }

                MediaPlayer = vlcMediaPlayer;
                player.PlaybackStateChanged += OnPlaybackStateChanged;
                player.PositionChanged += OnPositionChanged;
                player.MediaFailed += OnMediaFailed;
                player.PlaybackItemChanged += OnPlaybackItemChanged;

                initException?.Throw();
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.InitializationFailed, message: ex.Message));
                });
            }
        });
    }

    public void UpdatePlayerViewSize(Size size)
    {
        _viewSize = size;
        SetCropGeometry(_aspectRatio);

        try
        {
            var displayInfo = DisplayInformation.GetForCurrentView();
            _rawPixelsPerViewPixel = displayInfo.RawPixelsPerViewPixel;
        }
        catch (Exception e)
        {
            _rawPixelsPerViewPixel = 1.0;
            _logger.LogError(e, "Failed to query the current display information.");
        }
    }

    public void OnClick()
    {
        if (_playerSettings.GestureTap is PlaybackActionKind.None || MediaPlayer?.PlaybackItem == null)
        {
            return;
        }

        if (_clickTimer.IsRunning)
        {
            _clickTimer.Stop();
            return;
        }

        _clickTimer.Debounce(() => ProcessPlayerGesture(_playerSettings.GestureTap, GestureStepAmount), TimeSpan.FromMilliseconds(200));
    }

    public void OnManipulationCompleted()
    {
        _slideOrientation = SlideOrientation.None;
        Messenger.Send(new OverrideControlsHideDelayMessage(100));
        Messenger.Send(new TimeChangeOverrideMessage(false));
    }

    /// <summary>
    /// Interprets a pointer wheel input and requests changes to the current playback.
    /// </summary>
    /// <remarks>
    /// The pointer wheel vertical component adjusts the playback volume, while the
    /// horizontal component seeks the current media playback position.
    /// </remarks>
    /// <param name="delta">The pointer wheel delta.</param>
    /// <param name="isHorizontal"><see langword="true"/> to treat the input as horizontal;
    /// otherwise, <see langword="false"/> to treat it as vertical.</param>
    /// <param name="isTouchpad"><see langword="true"/> if the input originated from a touchpad;
    /// <see langword="false"/> if from a mouse; or <see langword="null"/> to detect automatically via delta heuristics.</param>
    public void ProcessPointerWheelInput(int delta, bool isHorizontal, bool? isTouchpad = null)
    {
        if (!isHorizontal)
        {
            bool touchpad = isTouchpad ?? TouchpadHelper.IsTouchpadDelta(delta);
            int volumeStep = touchpad ? (delta < 0 ? 2 : -2) : (delta > 0 ? 2 : -2);
            int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeStep, true));
            Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Volume, value: volume).ShowMessage());
        }
        else if (MediaPlayer?.CanSeek ?? false)
        {
            // Pointer wheel events can be fired in quick succession and still count as a single seek action from the user's perspective.
            // Debounce setting the time before manipulation to calculate the culmulative change correctly.
            _pointerWheelTimer.Debounce(() => _timeBeforeManipulation = MediaPlayer.Position, TimeSpan.FromSeconds(1), true);
            Messenger.Send(new TimeChangeOverrideMessage(true));
            // For mouse wheel, each detent is 120. For touchpad, it can be a smaller value.
            var seekAmount = -(delta / 12.0);   // Each mouse wheel detent corresponds to a 10 second seek. Touchpad has more precision.
            seekAmount = Math.Abs(seekAmount) < 0.5 ? Math.Sign(seekAmount) * 0.5 : seekAmount;
            var newTime = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromSeconds(seekAmount), true)).Response.NewPosition;
            UpdateTimeStatusMessage(newTime);
        }
    }

    /// <summary>
    /// Interprets a swipe manipulation and requests the corresponding playback interaction.
    /// </summary>
    /// <param name="cumulative">The cumulative translation of the pointer manipulation.</param>
    public void ProcessSwipeGesture(Point cumulative)
    {
        const double SwipeThreshold = 100.0;

        if (IsHolding) return;

        double absoluteCumulativeX = Math.Abs(cumulative.X);
        double absoluteCumulativeY = Math.Abs(cumulative.Y);
        var gesture = PlaybackActionKind.None;

        if (absoluteCumulativeX > absoluteCumulativeY && absoluteCumulativeX >= SwipeThreshold && !_playerSettings.GestureSlideHorizontal)
        {
            gesture = cumulative.X > 0 ? _playerSettings.GestureSwipeRight : _playerSettings.GestureSwipeLeft;
        }
        else if (absoluteCumulativeY > absoluteCumulativeX && absoluteCumulativeY >= SwipeThreshold && !_playerSettings.GestureSlideVertical)
        {
            gesture = cumulative.Y > 0 ? _playerSettings.GestureSwipeDown : _playerSettings.GestureSwipeUp;
        }

        if (gesture is PlaybackActionKind.None) return;

        ProcessPlayerGesture(gesture, GestureStepAmount);
    }

    /// <summary>
    /// Interprets a slide manipulation and requests the corresponding playback interaction.
    /// </summary>
    /// <param name="delta">The change in pointer position since the last event.</param>
    /// <param name="cumulative">The cumulative translation of the pointer manipulation.</param>
    /// <remarks>
    /// The vertical slide gesture adjusts the playback volume, and the horizontal
    /// slide gesture performs a time seek relative to the current position.
    /// </remarks>
    public void ProcessSlideGesture(Point delta, Point cumulative)
    {
        const double HorizontalStepAmountPerPixel = 200;
        const double VerticalStepAmountPerPixel = 1;

        if (IsHolding) return;

        double absCumulativeX = Math.Abs(cumulative.X);
        double absCumulativeY = Math.Abs(cumulative.Y);

        if (_slideOrientation is SlideOrientation.None)
        {
            if (absCumulativeY > absCumulativeX && absCumulativeY >= 50 && _playerSettings.GestureSlideVertical)
            {
                _slideOrientation = SlideOrientation.Vertical;
            }
            else if (absCumulativeX > absCumulativeY && absCumulativeX >= 50 && _playerSettings.GestureSlideHorizontal)
            {
                _slideOrientation = SlideOrientation.Horizontal;
                if (MediaPlayer != null)
                    _timeBeforeManipulation = MediaPlayer.Position;
            }
        }

        if (_slideOrientation is SlideOrientation.Vertical)
        {
            int volume = Messenger.Send(new ChangeVolumeRequestMessage((int)-(VerticalStepAmountPerPixel / _rawPixelsPerViewPixel * delta.Y), true));
            Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Volume, value: volume).ShowMessage());
        }
        else if (_slideOrientation is SlideOrientation.Horizontal && (MediaPlayer?.CanSeek ?? false))
        {
            Messenger.Send(new TimeChangeOverrideMessage(true));
            TimeSpan timeChange = TimeSpan.FromMilliseconds(HorizontalStepAmountPerPixel / _rawPixelsPerViewPixel * delta.X);
            TimeSpan newTime = Messenger.Send(new ChangeTimeRequestMessage(timeChange, true)).Response.NewPosition;
            UpdateTimeStatusMessage(newTime);
        }
    }

    /// <summary>
    /// Interprets a holding gesture and adjusts the playback rate based on the state.
    /// </summary>
    /// <remarks>Increases the playback rate while holding, and restores it on release.</remarks>
    /// <param name="holdingState">A value of the enumeration that specifies the current
    /// state of the holding gesture.</param>
    public void ProcessHoldingGesture(HoldingState holdingState)
    {
        const double HoldingSpeed = 2.0;

        if (!_playerSettings.GesturePressAndHold || MediaPlayer is null) return;
        if (!IsHolding && MediaPlayer.PlaybackState is MediaPlaybackState.Paused) return;

        switch (holdingState)
        {
            case HoldingState.Started when !IsHolding:
                _playbackRateBeforeHold = MediaPlayer.PlaybackRate;
                // If the rate is already faster than the holding speed, set it to twice the holding speed.
                double effectiveHoldingSpeed = MediaPlayer.PlaybackRate >= HoldingSpeed ? HoldingSpeed * 2.0 : HoldingSpeed;
                if (MediaPlayer.PlaybackRate != effectiveHoldingSpeed)
                {
                    Messenger.Send(new ChangePlaybackRateRequestMessage(effectiveHoldingSpeed));
                    Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.RateUp, value: effectiveHoldingSpeed).SetDuration(Timeout.InfiniteTimeSpan).ShowMessage());
                }

                IsHolding = true;
                break;
            case HoldingState.Completed when IsHolding:
            case HoldingState.Canceled when IsHolding:
                if (MediaPlayer.PlaybackRate != _playbackRateBeforeHold)
                {
                    Messenger.Send(new ChangePlaybackRateRequestMessage(_playbackRateBeforeHold));
                    Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.RateDown, value: _playbackRateBeforeHold).ShowMessage());
                }

                IsHolding = false;
                break;
        }
    }

    private void ProcessPlayerGesture(PlaybackActionKind gestureOption, double change)
    {
        if (MediaPlayer is null) return;

        double playbackRate = MediaPlayer.PlaybackRate;
        double rateDelta = change / 20.0;

        switch (gestureOption)
        {
            case PlaybackActionKind.None:
                return;
            case PlaybackActionKind.PlayPause:
                Messenger.Send(new TogglePlayPauseMessage(true));
                break;
            case PlaybackActionKind.Rewind:
                if (MediaPlayer?.CanSeek ?? false)
                {
                    _timeBeforeManipulation = MediaPlayer.Position;
                    Messenger.Send(new TimeChangeOverrideMessage(true));
                    var newTime = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromSeconds(-_playerSettings.RewindStep), true)).Response.NewPosition;
                    UpdateTimeStatusMessage(newTime);
                }
                break;
            case PlaybackActionKind.FastForward:
                if (MediaPlayer?.CanSeek ?? false)
                {
                    _timeBeforeManipulation = MediaPlayer.Position;
                    Messenger.Send(new TimeChangeOverrideMessage(true));
                    var newTime = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromSeconds(_playerSettings.FastForwardStep), true)).Response.NewPosition;
                    UpdateTimeStatusMessage(newTime);
                }
                break;
            case PlaybackActionKind.DecreaseVolume:
                int volumeDown = Messenger.Send(new ChangeVolumeRequestMessage((int)-change, true));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Volume, value: volumeDown).ShowMessage());
                break;
            case PlaybackActionKind.IncreaseVolume:
                int volumeUp = Messenger.Send(new ChangeVolumeRequestMessage((int)change, true));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Volume, value: volumeUp).ShowMessage());
                break;
            case PlaybackActionKind.DecreaseRate:
                double rateDown = Messenger.Send(new ChangePlaybackRateRequestMessage(Math.Clamp(playbackRate - rateDelta, 0.25, 4)));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.RateDown, value: rateDown).ShowMessage());
                break;
            case PlaybackActionKind.IncreaseRate:
                double rateUp = Messenger.Send(new ChangePlaybackRateRequestMessage(Math.Clamp(playbackRate + rateDelta, 0.25, 4)));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.RateUp, value: rateUp).ShowMessage());
                break;
            case PlaybackActionKind.PreviousTrack:
                Messenger.Send(new TrackNavigationMessage(TrackNavigationDirection.Previous));
                break;
            case PlaybackActionKind.NextTrack:
                Messenger.Send(new TrackNavigationMessage(TrackNavigationDirection.Next));
                break;
            case PlaybackActionKind.PreviousChapter:
                MediaPlayer.PreviousChapter();
                break;
            case PlaybackActionKind.NextChapter:
                MediaPlayer.NextChapter();
                break;
        }
    }

    private void OnMediaFailed(IMediaPlayer sender, object? args)
    {
        _transportControlsService.ClosePlayback();
    }

    private void OnPositionChanged(IMediaPlayer sender, object? args)
    {
        _transportControlsService.UpdatePlaybackPosition(sender.Position, TimeSpan.Zero, sender.NaturalDuration);
    }

    private void OnPlaybackItemChanged(IMediaPlayer sender, ValueChangedEventArgs<IPlaybackItem?> args)
    {
        if (args.NewValue == null) ClearViewRequested?.Invoke(this, EventArgs.Empty);
    }

    private void TransportControlsOnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
    {
        if (MediaPlayer == null) return;
        MediaPlayer.Position = args.RequestedPlaybackPosition;
    }

    private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        if (MediaPlayer == null) return;
        switch (args.Button)
        {
            case SystemMediaTransportControlsButton.Pause:
                MediaPlayer.Pause();
                break;
            case SystemMediaTransportControlsButton.Play:
                MediaPlayer.Play();
                break;
            case SystemMediaTransportControlsButton.Stop:
                MediaPlayer.PlaybackItem = null;
                break;
            case SystemMediaTransportControlsButton.FastForward:
                MediaPlayer.Position += TimeSpan.FromSeconds(10);
                break;
            case SystemMediaTransportControlsButton.Rewind:
                MediaPlayer.Position -= TimeSpan.FromSeconds(10);
                break;
        }
    }

    private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            UpdateDisplayRequest(sender.PlaybackState, _requestTracker);
        });

        _transportControlsService.UpdatePlaybackStatus(sender.PlaybackState);
    }

    private void SetCropGeometry(Size size)
    {
        if (MediaPlayer == null || size.Width < 0 || size.Height < 0) return;
        Rect defaultSize = new(0, 0, 1, 1);
        if (size is { Width: 0, Height: 0 })
        {
            if (MediaPlayer.NormalizedSourceRect == defaultSize) return;
            MediaPlayer.NormalizedSourceRect = defaultSize;
        }
        else
        {
            if (double.IsNaN(size.Width) || double.IsNaN(size.Height))
            {
                size = _viewSize;
            }

            double leftOffset = 0.5, topOffset = 0.5;
            double widthRatio = size.Width / MediaPlayer.NaturalVideoWidth;
            double heightRatio = size.Height / MediaPlayer.NaturalVideoHeight;
            double ratio = Math.Max(widthRatio, heightRatio);
            double width = size.Width / ratio / MediaPlayer.NaturalVideoWidth;
            double height = size.Height / ratio / MediaPlayer.NaturalVideoHeight;
            leftOffset -= width / 2;
            topOffset -= height / 2;

            MediaPlayer.NormalizedSourceRect = new Rect(leftOffset, topOffset, width, height);
        }
    }

    private static void UpdateDisplayRequest(MediaPlaybackState state, DisplayRequestTracker tracker)
    {
        bool shouldActive = state
            is MediaPlaybackState.Playing
            or MediaPlaybackState.Buffering
            or MediaPlaybackState.Opening;
        if (shouldActive && !tracker.IsActive)
        {
            tracker.RequestActive();
        }
        else if (!shouldActive && tracker.IsActive)
        {
            tracker.RequestRelease();
        }
    }

    private void UpdateTimeStatusMessage(TimeSpan newTime)
    {
        TimeSpan delta = newTime - _timeBeforeManipulation;
        bool isDeltaPositive = delta.Ticks >= 0;

        var statusStrBuilder = new System.Text.StringBuilder(64);
        statusStrBuilder.Append(Humanizer.ToDuration(newTime))
            .Append(" (");

        if (isDeltaPositive)
            statusStrBuilder.Append('+');

        statusStrBuilder.Append(Humanizer.ToDuration(delta))
            .Append(')');

        Messenger.Send(
            new PlayerOsdUpdateMessage(
                isDeltaPositive ? PlaybackCommandKind.FastForward : PlaybackCommandKind.Rewind,
                value: statusStrBuilder.ToString())
            .ShowMessage());
    }
}


#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using LibVLCSharp.Shared;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Input;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlayerElementViewModel : ObservableRecipient,
    IRecipient<ChangeAspectRatioMessage>,
    IRecipient<SettingsChangedMessage>
{
    private const double GestureStepAmount = 5.0;

    [ObservableProperty] private bool _isHolding;

    public event EventHandler<EventArgs>? ClearViewRequested;

    public MediaPlayer? VlcPlayer => VlcMediaPlayer?.VlcPlayer;

    private VlcMediaPlayer? VlcMediaPlayer
    {
        get => _playerContext.MediaPlayer as VlcMediaPlayer;
        set => _playerContext.MediaPlayer = value;
    }

    private readonly PlayerContext _playerContext;
    private readonly IPlayerService _playerService;
    private readonly ISystemMediaTransportControlsService _transportControlsService;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _clickTimer;
    private readonly DisplayRequestTracker _requestTracker;
    private Size _viewSize;
    private Size _aspectRatio;
    private TimeSpan _timeBeforeManipulation;
    private PlaybackActionKind _playerGestureTap;
    private PlaybackActionKind _playerGestureSwipeUp;
    private PlaybackActionKind _playerGestureSwipeDown;
    private PlaybackActionKind _playerGestureSwipeLeft;
    private PlaybackActionKind _playerGestureSwipeRight;
    private bool _playerGesturePressAndHold;
    private double? _playbackRateBeforeHold;
    private bool _suppressNextTap;

    public PlayerElementViewModel(
        PlayerContext playerContext,
        IPlayerService playerService,
        ISettingsService settingsService,
        ISystemMediaTransportControlsService transportControlsService)
    {
        _playerContext = playerContext;
        _playerService = playerService;
        _settingsService = settingsService;
        _transportControlsService = transportControlsService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _clickTimer = _dispatcherQueue.CreateTimer();
        _requestTracker = new DisplayRequestTracker();
        LoadSettings();

        transportControlsService.TransportControls.ButtonPressed += TransportControlsOnButtonPressed;
        transportControlsService.TransportControls.PlaybackPositionChangeRequested += TransportControlsOnPlaybackPositionChangeRequested;

        // View model does not receive any message
        IsActive = true;
    }

    public void Receive(SettingsChangedMessage message)
    {
        LoadSettings();
    }

    public void Receive(ChangeAspectRatioMessage message)
    {
        _aspectRatio = message.Value;
        SetCropGeometry(message.Value);
    }

    public void Initialize(string[] swapChainOptions)
    {
        if (VlcMediaPlayer != null)
        {
            var oldPlayer = VlcMediaPlayer;
            oldPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
            oldPlayer.PositionChanged -= OnPositionChanged;
            oldPlayer.MediaFailed -= OnMediaFailed;
            oldPlayer.PlaybackItemChanged -= OnPlaybackItemChanged;
            _playerService.DisposePlayer(oldPlayer);
            VlcMediaPlayer = null;
        }

        // Try to initialize the player in a background thread to avoid blocking the UI
        Task.Run(() =>
        {
            try
            {
                var args = new List<string>();
                if (_settingsService.GlobalArguments.Length > 0)
                {
                    args.AddRange(_settingsService.GlobalArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }

                if (_settingsService.VideoUpscale != VideoUpscaleOption.Linear)
                {
                    args.Add($"--d3d11-upscale-mode={_settingsService.VideoUpscale.ToString().ToLower()}");
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

                VlcMediaPlayer = vlcMediaPlayer;
                player.PlaybackStateChanged += OnPlaybackStateChanged;
                player.PositionChanged += OnPositionChanged;
                player.MediaFailed += OnMediaFailed;
                player.PlaybackItemChanged += OnPlaybackItemChanged;

                if (initException != null)
                    initException.Throw();
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Messenger.Send(new FailedToInitializeNotificationMessage(ex.Message));
                });
            }

        });
    }

    public void UpdatePlayerViewSize(Size size)
    {
        _viewSize = size;
        SetCropGeometry(_aspectRatio);
    }

    public void OnClick()
    {
        if (_settingsService.PlayerGestureTap is PlaybackActionKind.None || VlcMediaPlayer?.PlaybackItem == null)
        {
            return;
        }

        if (_suppressNextTap)
        {
            _suppressNextTap = false;
            return;
        }

        if (_clickTimer.IsRunning)
        {
            _clickTimer.Stop();
            return;
        }
        _clickTimer.Debounce(() => ProcessPlayerGesture(_playerGestureTap, GestureStepAmount), TimeSpan.FromMilliseconds(200));
    }

    public void OnManipulationCompleted()
    {
        Messenger.Send(new OverrideControlsHideDelayMessage(100));
        Messenger.Send(new TimeChangeOverrideMessage(false));
    }

    /// <summary>
    /// Interprets a pointer wheel input and requests changes to the current playback.
    /// </summary>
    /// <remarks>
    /// The pointer wheel vertical component adjusts the playback volume, its horizontal
    /// component seeks the current media playback position.
    /// </remarks>
    /// <param name="delta">The pointer wheel delta.</param>
    /// <param name="isHorizontal"><see langword="true"/> to treat the input as horizontal;
    /// otherwise, treat it as vertical.</param>
    public void ProcessPointerWheelInput(int delta, bool isHorizontal)
    {
        if (!isHorizontal)
        {
            int volume = Messenger.Send(new ChangeVolumeRequestMessage(delta > 0 ? 5 : -5, true));
            Messenger.Send(new UpdateVolumeStatusMessage(volume));
        }
        else
        {
            if (VlcMediaPlayer?.CanSeek ?? false)
            {
                _timeBeforeManipulation = VlcMediaPlayer.Position;
                Messenger.Send(new TimeChangeOverrideMessage(true));
                var newTime = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromSeconds(delta > 0 ? -5 : 5), true)).Response.NewPosition;
                UpdateTimeStatusMessage(newTime);
            }
        }
    }

    /// <summary>
    /// Interprets a swipe manipulation and requests the corresponding playback interaction.
    /// </summary>
    /// <param name="cumulative">The cumulative translation of the pointer manipulation.
    /// The <see cref="Point.X"/> component represents horizontal movement,
    /// and the <see cref="Point.Y"/> component represents vertical movement.</param>
    public void ProcessSwipeGesture(Point cumulative)
    {
        const double SwipeThreshold = 100.0;

        if (VlcMediaPlayer != null)
        {
            _timeBeforeManipulation = VlcMediaPlayer.Position;
        }

        double absoluteCumulativeX = Math.Abs(cumulative.X);
        double absoluteCumulativeY = Math.Abs(cumulative.Y);
        var gesture = PlaybackActionKind.None;

        if (absoluteCumulativeX > absoluteCumulativeY && absoluteCumulativeX >= SwipeThreshold)
        {
            gesture = cumulative.X > 0 ? _playerGestureSwipeRight : _playerGestureSwipeLeft;
        }
        else if (absoluteCumulativeY > absoluteCumulativeX && absoluteCumulativeY >= SwipeThreshold)
        {
            gesture = cumulative.Y > 0 ? _playerGestureSwipeDown : _playerGestureSwipeUp;
        }

        if (gesture is PlaybackActionKind.None) return;

        ProcessPlayerGesture(gesture, GestureStepAmount);
    }

    /// <summary>
    /// Interprets a holding gesture and adjusts the playback rate based on the state.
    /// </summary>
    /// <remarks>Increases the playback rate while holding, and restores it on release.</remarks>
    /// <param name="holdingState">The current state of the holding gesture.</param>
    public void ProcessHoldingGesture(HoldingState holdingState)
    {
        const double HoldingSpeed = 2.0;

        if (!_playerGesturePressAndHold || VlcMediaPlayer is null || VlcMediaPlayer.PlaybackState is MediaPlaybackState.Paused) return;

        switch (holdingState)
        {
            case HoldingState.Started:
                if (!IsHolding)
                {
                    _playbackRateBeforeHold = VlcMediaPlayer.PlaybackRate;
                    _suppressNextTap = true;
                    // If the rate is already faster than the holding speed, set it to twice the holding speed.
                    double effectiveHoldingSpeed = VlcMediaPlayer.PlaybackRate >= HoldingSpeed ? HoldingSpeed * 2.0 : HoldingSpeed;
                    if (VlcMediaPlayer.PlaybackRate != effectiveHoldingSpeed)
                    {
                        Messenger.Send(new ChangePlaybackRateRequestMessage(effectiveHoldingSpeed));
                        Messenger.Send(new UpdateStatusMessage(FormatPlaybackRate(effectiveHoldingSpeed), System.Threading.Timeout.InfiniteTimeSpan));
                    }
                    IsHolding = true;
                }
                break;
            case HoldingState.Completed:
            case HoldingState.Canceled:
                if (IsHolding)
                {
                    if (_playbackRateBeforeHold.HasValue && VlcMediaPlayer.PlaybackRate != _playbackRateBeforeHold.Value)
                    {
                        Messenger.Send(new ChangePlaybackRateRequestMessage(_playbackRateBeforeHold.Value));
                        Messenger.Send(new UpdateStatusMessage(FormatPlaybackRate(_playbackRateBeforeHold.Value)));
                    }
                    _playbackRateBeforeHold = null;
                    IsHolding = false;
                }
                break;
        }
    }

    private void ProcessPlayerGesture(PlaybackActionKind gestureOption, double change)
    {
        if (VlcMediaPlayer is null) return;

        double playbackRate = VlcMediaPlayer.PlaybackRate;
        double rateDelta = change / 20.0;

        switch (gestureOption)
        {
            case PlaybackActionKind.None:
                return;
            case PlaybackActionKind.PlayPause:
                Messenger.Send(new TogglePlayPauseMessage(true));
                break;
            case PlaybackActionKind.Rewind:
                if (VlcMediaPlayer?.CanSeek ?? false)
                {
                    Messenger.Send(new TimeChangeOverrideMessage(true));
                    var newTime = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromSeconds(-change), true)).Response.NewPosition;
                    UpdateTimeStatusMessage(newTime);
                }
                break;
            case PlaybackActionKind.FastForward:
                if (VlcMediaPlayer?.CanSeek ?? false)
                {
                    Messenger.Send(new TimeChangeOverrideMessage(true));
                    var newTime = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromSeconds(change), true)).Response.NewPosition;
                    UpdateTimeStatusMessage(newTime);
                }
                break;
            case PlaybackActionKind.DecreaseVolume:
                var volumeDown = Messenger.Send(new ChangeVolumeRequestMessage((int)-change, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volumeDown));
                break;
            case PlaybackActionKind.IncreaseVolume:
                var volumeUp = Messenger.Send(new ChangeVolumeRequestMessage((int)change, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volumeUp));
                break;
            case PlaybackActionKind.DecreaseRate:
                double rateDown = Messenger.Send(new ChangePlaybackRateRequestMessage(Math.Clamp(playbackRate - rateDelta, 0.25, 4)));
                Messenger.Send(new UpdateStatusMessage(FormatPlaybackRate(rateDown)));
                break;
            case PlaybackActionKind.IncreaseRate:
                double rateUp = Messenger.Send(new ChangePlaybackRateRequestMessage(Math.Clamp(playbackRate + rateDelta, 0.25, 4)));
                Messenger.Send(new UpdateStatusMessage(FormatPlaybackRate(rateUp)));
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

    private void OnPlaybackItemChanged(IMediaPlayer sender, ValueChangedEventArgs<PlaybackItem?> args)
    {
        if (args.NewValue == null) ClearViewRequested?.Invoke(this, EventArgs.Empty);
    }

    private void TransportControlsOnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
    {
        if (VlcMediaPlayer == null) return;
        VlcMediaPlayer.Position = args.RequestedPlaybackPosition;
    }

    private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        if (VlcMediaPlayer == null) return;
        switch (args.Button)
        {
            case SystemMediaTransportControlsButton.Pause:
                VlcMediaPlayer.Pause();
                break;
            case SystemMediaTransportControlsButton.Play:
                VlcMediaPlayer.Play();
                break;
            case SystemMediaTransportControlsButton.Stop:
                VlcMediaPlayer.PlaybackItem = null;
                break;
            case SystemMediaTransportControlsButton.FastForward:
                VlcMediaPlayer.Position += TimeSpan.FromSeconds(10);
                break;
            case SystemMediaTransportControlsButton.Rewind:
                VlcMediaPlayer.Position -= TimeSpan.FromSeconds(10);
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
        if (VlcMediaPlayer == null || size.Width < 0 || size.Height < 0) return;
        Rect defaultSize = new(0, 0, 1, 1);
        if (size is { Width: 0, Height: 0 })
        {
            if (VlcMediaPlayer.NormalizedSourceRect == defaultSize) return;
            VlcMediaPlayer.NormalizedSourceRect = defaultSize;
        }
        else
        {
            if (double.IsNaN(size.Width) || double.IsNaN(size.Height))
            {
                size = _viewSize;
            }

            double leftOffset = 0.5, topOffset = 0.5;
            double widthRatio = size.Width / VlcMediaPlayer.NaturalVideoWidth;
            double heightRatio = size.Height / VlcMediaPlayer.NaturalVideoHeight;
            double ratio = Math.Max(widthRatio, heightRatio);
            double width = size.Width / ratio / VlcMediaPlayer.NaturalVideoWidth;
            double height = size.Height / ratio / VlcMediaPlayer.NaturalVideoHeight;
            leftOffset -= width / 2;
            topOffset -= height / 2;

            VlcMediaPlayer.NormalizedSourceRect = new Rect(leftOffset, topOffset, width, height);
        }
    }

    private void LoadSettings()
    {
        _playerGestureTap = _settingsService.PlayerGestureTap;
        _playerGestureSwipeUp = _settingsService.PlayerGestureSwipeUp;
        _playerGestureSwipeDown = _settingsService.PlayerGestureSwipeDown;
        _playerGestureSwipeLeft = _settingsService.PlayerGestureSwipeLeft;
        _playerGestureSwipeRight = _settingsService.PlayerGestureSwipeRight;
        _playerGesturePressAndHold = _settingsService.PlayerGesturePressAndHold;
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
        var changeText = Humanizer.ToDuration(newTime - _timeBeforeManipulation);
        if (changeText[0] != '-')
        {
            changeText = "+" + changeText;
        }
        var status = $"{Humanizer.ToDuration(newTime)} ({changeText})";
        Messenger.Send(new UpdateStatusMessage(status));
    }

    private static string FormatPlaybackRate(double rate)
    {
        return $"{rate.ToString("0.##", CultureInfo.CurrentCulture)}×";
    }
}

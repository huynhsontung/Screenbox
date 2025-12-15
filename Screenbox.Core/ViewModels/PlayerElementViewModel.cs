#nullable enable

using System;
using System.Collections.Generic;
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

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PlayerElementViewModel : ObservableRecipient,
        IRecipient<ChangeAspectRatioMessage>,
        IRecipient<SettingsChangedMessage>
    {
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
        private readonly IResourceService _resourceService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _clickTimer;
        private readonly DisplayRequestTracker _requestTracker;
        private Size _viewSize;
        private Size _aspectRatio;
        private ManipulationLock _manipulationLock;
        private TimeSpan _timeBeforeManipulation;
        private MediaCommandType _playerTapGesture;
        private MediaCommandType _playerSwipeUpGesture;
        private MediaCommandType _playerSwipeDownGesture;
        private MediaCommandType _playerSwipeLeftGesture;
        private MediaCommandType _playerSwipeRightGesture;
        private bool _playerTapAndHoldGesture;
        private double _playbackRateBeforeHolding;
        private bool _suppressTap;

        public PlayerElementViewModel(
            PlayerContext playerContext,
            IPlayerService playerService,
            ISettingsService settingsService,
            ISystemMediaTransportControlsService transportControlsService,
            IResourceService resourceService)
        {
            _playerContext = playerContext;
            _playerService = playerService;
            _settingsService = settingsService;
            _transportControlsService = transportControlsService;
            _resourceService = resourceService;
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
                var player = VlcMediaPlayer;
                player.PlaybackStateChanged -= OnPlaybackStateChanged;
                player.PositionChanged -= OnPositionChanged;
                player.MediaFailed -= OnMediaFailed;
                player.PlaybackItemChanged -= OnPlaybackItemChanged;
                _playerService.DisposePlayer(player);
                VlcMediaPlayer = null;
            }

            Task.Run(() =>
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
                try
                {
                    player = _playerService.Initialize(args.ToArray());
                }
                catch (VLCException e)
                {
                    player = _playerService.Initialize(swapChainOptions);
                    Messenger.Send(new ErrorMessage(
                        _resourceService.GetString(ResourceName.FailedToInitializeNotificationTitle), e.Message));
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
            });
        }

        public void UpdatePlayerViewSize(Size size)
        {
            _viewSize = size;
            SetCropGeometry(_aspectRatio);
        }

        public void OnClick()
        {
            if (VlcMediaPlayer?.PlaybackItem == null) return;
            if (_suppressTap)
            {
                _suppressTap = false;
                return;
            }

            if (_clickTimer.IsRunning)
            {
                _clickTimer.Stop();
                return;
            }
            _clickTimer.Debounce(() => ProcessMediaGesture(_playerTapGesture, 10.0, 0.0), TimeSpan.FromMilliseconds(200));
        }

        public void ManipulationStarted()
        {
            _manipulationLock = ManipulationLock.None;
        }

        public void ManipulationCompleted()
        {
            if (_manipulationLock == ManipulationLock.None) return;
            Messenger.Send(new OverrideControlsHideDelayMessage(100));
            Messenger.Send(new TimeChangeOverrideMessage(false));
        }

        public void HandlePointerWheelInput(int delta, bool isHorizontal)
        {
            if (!isHorizontal)
            {
                int volume = Messenger.Send(new ChangeVolumeRequestMessage(delta, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volume));
            }
            else
            {
                if (VlcMediaPlayer?.CanSeek == true)
                {
                    _timeBeforeManipulation = VlcMediaPlayer.Position;
                    Messenger.Send(new TimeChangeOverrideMessage(true));
                    var timeChange = TimeSpan.FromSeconds(-delta);
                    var newTime = Messenger.Send(new ChangeTimeRequestMessage(timeChange, true)).Response.NewPosition;
                    UpdateTimeStatusMessage(newTime);
                }
            }
        }

        public void HandleManipulationGesture(
            double horizontalDelta,
            double verticalDelta,
            double horizontalCumulative,
            double verticalCumulative)
        {
            if (VlcMediaPlayer != null && _manipulationLock == ManipulationLock.None)
            {
                _timeBeforeManipulation = VlcMediaPlayer.Position;
            }

            // Vertical gestures
            if (_manipulationLock != ManipulationLock.Horizontal && Math.Abs(verticalCumulative) >= 2)
            {
                _manipulationLock = ManipulationLock.Vertical;
                ProcessVerticalGesture(verticalDelta, verticalCumulative);
                return;
            }

            // Horizontal gestures
            if (_manipulationLock != ManipulationLock.Vertical && Math.Abs(horizontalCumulative) >= 2)
            {
                _manipulationLock = ManipulationLock.Horizontal;
                ProcessHorizontalGesture(horizontalDelta, horizontalCumulative);
                return;
            }
        }

        public void HandleHoldingGesture(HoldingState holdingState)
        {
            const double holdingSpeed = 2.0;

            if (!_playerTapAndHoldGesture || VlcMediaPlayer == null) return;

            switch (holdingState)
            {
                case HoldingState.Started:
                    if (!IsHolding)
                    {
                        _playbackRateBeforeHolding = VlcMediaPlayer.PlaybackRate;
                        _suppressTap = true;
                        if (VlcMediaPlayer.PlaybackRate != holdingSpeed)
                        {
                            SetPlaybackSpeed(holdingSpeed);
                        }
                        IsHolding = true;
                    }
                    break;
                case HoldingState.Completed:
                case HoldingState.Canceled:
                    if (IsHolding)
                    {
                        if (VlcMediaPlayer.PlaybackRate != _playbackRateBeforeHolding)
                        {
                            SetPlaybackSpeed(_playbackRateBeforeHolding);
                        }
                        IsHolding = false;
                    }
                    break;
                default:
                    break;
            }
        }

        private void ProcessMediaGesture(MediaCommandType gestureKind, double change, double cumulative)
        {
            const double ChangePerPixel = 200;

            switch (gestureKind)
            {
                case MediaCommandType.None:
                    return;
                case MediaCommandType.PlayPause:
                    Messenger.Send(new TogglePlayPauseMessage(true));
                    break;
                case MediaCommandType.Rewind:
                    if (VlcMediaPlayer?.CanSeek == true)
                    {
                        Messenger.Send(new TimeChangeOverrideMessage(true));
                        var timeChange = TimeSpan.FromMilliseconds(-change * ChangePerPixel);
                        var newTime = Messenger.Send(new ChangeTimeRequestMessage(timeChange, true)).Response.NewPosition;
                        UpdateTimeStatusMessage(newTime);
                    }
                    break;
                case MediaCommandType.FastForward:
                    if (VlcMediaPlayer?.CanSeek == true)
                    {
                        Messenger.Send(new TimeChangeOverrideMessage(true));
                        var timeChange = TimeSpan.FromMilliseconds(change * ChangePerPixel);
                        var newTime = Messenger.Send(new ChangeTimeRequestMessage(timeChange, true)).Response.NewPosition;
                        UpdateTimeStatusMessage(newTime);
                    }
                    break;
                case MediaCommandType.DecreaseVolume:
                    var volumeDown = Messenger.Send(new ChangeVolumeRequestMessage((int)-change, true));
                    Messenger.Send(new UpdateVolumeStatusMessage(volumeDown));
                    break;
                case MediaCommandType.IncreaseVolume:
                    var volumeUp = Messenger.Send(new ChangeVolumeRequestMessage((int)change, true));
                    Messenger.Send(new UpdateVolumeStatusMessage(volumeUp));
                    break;
            }
        }

        private void ProcessVerticalGesture(double delta, double cumulative)
        {
            if (delta > 0)
            {
                ProcessMediaGesture(_playerSwipeDownGesture, delta, cumulative);
            }
            else
            {
                ProcessMediaGesture(_playerSwipeUpGesture, -delta, -cumulative);
            }
        }

        private void ProcessHorizontalGesture(double delta, double cumulative)
        {
            if (delta > 0)
            {
                ProcessMediaGesture(_playerSwipeRightGesture, delta, cumulative);
            }
            else
            {
                ProcessMediaGesture(_playerSwipeLeftGesture, -delta, -cumulative);
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
            _playerTapGesture = _settingsService.PlayerTapGesture;
            _playerSwipeUpGesture = _settingsService.PlayerSwipeUpGesture;
            _playerSwipeDownGesture = _settingsService.PlayerSwipeDownGesture;
            _playerSwipeLeftGesture = _settingsService.PlayerSwipeLeftGesture;
            _playerSwipeRightGesture = _settingsService.PlayerSwipeRightGesture;
            _playerTapAndHoldGesture = _settingsService.PlayerTapAndHoldGesture;
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

        private void SetPlaybackSpeed(double value)
        {
            if (VlcMediaPlayer != null)
            {
                VlcMediaPlayer.PlaybackRate = value;
                Messenger.Send(new UpdateStatusMessage($"{value}×"));
            }
        }
    }
}

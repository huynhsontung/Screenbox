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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PlayerElementViewModel : ObservableRecipient,
        IRecipient<ChangeAspectRatioMessage>,
        IRecipient<SettingsChangedMessage>,
        IRecipient<MediaPlayerRequestMessage>
    {
        public event EventHandler<EventArgs>? ClearViewRequested;

        public MediaPlayer? VlcPlayer { get; private set; }

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
        private VlcMediaPlayer? _mediaPlayer;
        private ManipulationLock _manipulationLock;
        private TimeSpan _timeBeforeManipulation;
        private bool _playerSeekGesture;
        private bool _playerVolumeGesture;

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

        public void Receive(MediaPlayerRequestMessage message)
        {
            message.Reply(_mediaPlayer);
        }

        public void Initialize(string[] swapChainOptions)
        {
            if (_mediaPlayer != null)
            {
                var player = _mediaPlayer;
                player.PlaybackStateChanged -= OnPlaybackStateChanged;
                player.PositionChanged -= OnPositionChanged;
                player.MediaFailed -= OnMediaFailed;
                player.PlaybackItemChanged -= OnPlaybackItemChanged;
                DisposeMediaPlayer();
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

                _mediaPlayer = vlcMediaPlayer;
                VlcPlayer = _mediaPlayer.VlcPlayer;
                player.PlaybackStateChanged += OnPlaybackStateChanged;
                player.PositionChanged += OnPositionChanged;
                player.MediaFailed += OnMediaFailed;
                player.PlaybackItemChanged += OnPlaybackItemChanged;
                Messenger.Send(new MediaPlayerChangedMessage(player));
            });
        }

        private void OnPlaybackItemChanged(IMediaPlayer sender, ValueChangedEventArgs<PlaybackItem?> args)
        {
            if (args.NewValue == null) ClearViewRequested?.Invoke(this, EventArgs.Empty);
        }

        public void OnClick()
        {
            if (!_settingsService.PlayerTapGesture || _mediaPlayer?.PlaybackItem == null) return;
            if (_clickTimer.IsRunning)
            {
                _clickTimer.Stop();
                return;
            }

            _clickTimer.Debounce(() => Messenger.Send(new TogglePlayPauseMessage(true)), TimeSpan.FromMilliseconds(200));
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            int volume = Messenger.Send(new ChangeVolumeRequestMessage(mouseWheelDelta > 0 ? 5 : -5, true));
            Messenger.Send(new UpdateVolumeStatusMessage(volume));
        }

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_manipulationLock == ManipulationLock.None) return;
            Messenger.Send(new OverrideControlsHideDelayMessage(100));
            Messenger.Send(new TimeChangeOverrideMessage(false));
        }

        public void VideoView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            const double horizontalChangePerPixel = 200;
            double horizontalChange = e.Delta.Translation.X;
            double verticalChange = e.Delta.Translation.Y;
            double horizontalCumulative = e.Cumulative.Translation.X;
            double verticalCumulative = e.Cumulative.Translation.Y;

            if (_mediaPlayer != null && _manipulationLock == ManipulationLock.None)
                _timeBeforeManipulation = _mediaPlayer.Position;

            if ((_manipulationLock == ManipulationLock.Vertical ||
                _manipulationLock == ManipulationLock.None && Math.Abs(verticalCumulative) >= 50) &&
                _playerVolumeGesture)
            {
                _manipulationLock = ManipulationLock.Vertical;
                int volume = Messenger.Send(new ChangeVolumeRequestMessage((int)-verticalChange, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volume));
                return;
            }

            if ((_manipulationLock == ManipulationLock.Horizontal ||
                 _manipulationLock == ManipulationLock.None && Math.Abs(horizontalCumulative) >= 50) &&
                (_mediaPlayer?.CanSeek ?? false) &&
                _playerSeekGesture)
            {
                _manipulationLock = ManipulationLock.Horizontal;
                Messenger.Send(new TimeChangeOverrideMessage(true));
                TimeSpan timeChange = TimeSpan.FromMilliseconds(horizontalChange * horizontalChangePerPixel);
                TimeSpan newTime = Messenger.Send(new ChangeTimeRequestMessage(timeChange, true)).Response.NewPosition;

                string changeText = Humanizer.ToDuration(newTime - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                string status = $"{Humanizer.ToDuration(newTime)} ({changeText})";
                Messenger.Send(new UpdateStatusMessage(status));
            }
        }

        public void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _manipulationLock = ManipulationLock.None;
        }

        private void OnMediaFailed(IMediaPlayer sender, object? args)
        {
            _transportControlsService.ClosePlayback();
        }

        private void OnPositionChanged(IMediaPlayer sender, object? args)
        {
            _transportControlsService.UpdatePlaybackPosition(sender.Position, TimeSpan.Zero, sender.NaturalDuration);
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs args)
        {
            _viewSize = args.NewSize;
            SetCropGeometry(_aspectRatio);
        }

        private void TransportControlsOnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.Position = args.RequestedPlaybackPosition;
        }

        private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (_mediaPlayer == null) return;
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    _mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    _mediaPlayer.Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    _mediaPlayer.PlaybackItem = null;
                    break;
                case SystemMediaTransportControlsButton.FastForward:
                    _mediaPlayer.Position += TimeSpan.FromSeconds(10);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    _mediaPlayer.Position -= TimeSpan.FromSeconds(10);
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
            if (_mediaPlayer == null || size.Width < 0 || size.Height < 0) return;
            Rect defaultSize = new(0, 0, 1, 1);
            if (size is { Width: 0, Height: 0 })
            {
                if (_mediaPlayer.NormalizedSourceRect == defaultSize) return;
                _mediaPlayer.NormalizedSourceRect = defaultSize;
            }
            else
            {
                if (double.IsNaN(size.Width) || double.IsNaN(size.Height))
                {
                    size = _viewSize;
                }

                double leftOffset = 0.5, topOffset = 0.5;
                double widthRatio = size.Width / _mediaPlayer.NaturalVideoWidth;
                double heightRatio = size.Height / _mediaPlayer.NaturalVideoHeight;
                double ratio = Math.Max(widthRatio, heightRatio);
                double width = size.Width / ratio / _mediaPlayer.NaturalVideoWidth;
                double height = size.Height / ratio / _mediaPlayer.NaturalVideoHeight;
                leftOffset -= width / 2;
                topOffset -= height / 2;

                _mediaPlayer.NormalizedSourceRect = new Rect(leftOffset, topOffset, width, height);
            }
        }

        private void LoadSettings()
        {
            _playerSeekGesture = _settingsService.PlayerSeekGesture;
            _playerVolumeGesture = _settingsService.PlayerVolumeGesture;
        }

        private void DisposeMediaPlayer()
        {
            _mediaPlayer?.Close();
            _mediaPlayer?.LibVlc.Dispose();
            _mediaPlayer = null;
            VlcPlayer = null;
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
    }
}

#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PlayerElementViewModel : ObservableRecipient,
        IRecipient<ChangeZoomToFitMessage>,
        IRecipient<MediaPlayerRequestMessage>
    {
        public MediaPlayer? VlcPlayer { get; private set; }

        private readonly LibVlcService _libVlcService;
        private readonly IWindowService _windowService;
        private readonly ISystemMediaTransportControlsService _transportControlsService;
        private readonly ISettingsService _settingsService;
        private readonly DispatcherQueue _dispatcherQueue;
        private Size _viewSize;
        private bool _zoomToFit;
        private bool _forceResize;
        private VlcMediaPlayer? _mediaPlayer;
        private DisplayRequest? _displayRequest;

        public PlayerElementViewModel(
            LibVlcService libVlcService,
            IWindowService windowService,
            ISettingsService settingsService,
            ISystemMediaTransportControlsService transportControlsService)
        {
            _libVlcService = libVlcService;
            _windowService = windowService;
            _settingsService = settingsService;
            _transportControlsService = transportControlsService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            transportControlsService.TransportControls.ButtonPressed += TransportControlsOnButtonPressed;
            transportControlsService.TransportControls.PlaybackPositionChangeRequested += TransportControlsOnPlaybackPositionChangeRequested;

            // View model does not receive any message
            IsActive = true;
        }

        public void Receive(ChangeZoomToFitMessage message)
        {
            _zoomToFit = message.Value;
            SetCropGeometry(_viewSize);
        }

        public void Receive(MediaPlayerRequestMessage message)
        {
            message.Reply(_mediaPlayer);
        }

        public void Initialize(string[] swapChainOptions)
        {
            Task.Run(() =>
            {
                _libVlcService.Initialize(swapChainOptions);
                _mediaPlayer = _libVlcService.MediaPlayer;
                Guard.IsNotNull(_mediaPlayer, nameof(_mediaPlayer));
                VlcPlayer = _mediaPlayer.VlcPlayer;
                _mediaPlayer.NaturalVideoSizeChanged += OnVideoSizeChanged;
                _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
                _mediaPlayer.PositionChanged += OnPositionChanged;
                _mediaPlayer.MediaFailed += OnMediaFailed;
                Messenger.Send(new MediaPlayerChangedMessage(_mediaPlayer));
                if (_settingsService.PlayerAutoResize == PlayerAutoResizeOption.OnLaunch)
                    _forceResize = true;
            });
        }

        public void OnResizeAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (sender.Modifiers != VirtualKeyModifiers.None) return;
            args.Handled = true;
            switch (sender.Key)
            {
                case VirtualKey.Number1:
                    ResizeWindow(0.5);
                    break;
                case VirtualKey.Number2:
                    ResizeWindow(1);
                    break;
                case VirtualKey.Number3:
                    ResizeWindow(2);
                    break;
                case VirtualKey.Number4:
                    ResizeWindow(0);
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

        public void OnSizeChanged(object sender, SizeChangedEventArgs args)
        {
            _viewSize = args.NewSize;
            SetCropGeometry(_viewSize);
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
                    _mediaPlayer.Source = null;
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
            if (sender.NaturalVideoHeight > 0 &&
                sender.PlaybackState == MediaPlaybackState.Playing &&
                _displayRequest == null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _displayRequest?.RequestRelease();
                    DisplayRequest request = _displayRequest = new DisplayRequest();
                    request.RequestActive();
                });
            }

            if ((sender.NaturalVideoHeight <= 0 ||
                sender.PlaybackState != MediaPlaybackState.Playing) &&
                _displayRequest != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _displayRequest?.RequestRelease();
                    _displayRequest = null;
                });
            }

            _transportControlsService.UpdatePlaybackStatus(sender.PlaybackState);
        }

        private void OnVideoSizeChanged(IMediaPlayer sender, object? args)
        {
            if (!_forceResize && _settingsService.PlayerAutoResize != PlayerAutoResizeOption.Always) return;
            _forceResize = false;

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ResizeWindow(1)) return;
                ResizeWindow();
            });
        }

        private bool ResizeWindow(double scalar = 0)
        {
            if (_mediaPlayer == null || scalar < 0 || _windowService.ViewMode != WindowViewMode.Default) return false;
            Size videoDimension = new(_mediaPlayer.NaturalVideoWidth, _mediaPlayer.NaturalVideoHeight);
            double actualScalar = _windowService.ResizeWindow(videoDimension, scalar);
            if (actualScalar > 0)
            {
                Messenger.Send(new UpdateStatusMessage($"Scale {actualScalar * 100:0.##}%"));
                return true;
            }

            return false;
        }

        private void SetCropGeometry(Size size)
        {
            if (_mediaPlayer == null) return;
            Rect defaultSize = new Rect(0, 0, 1, 1);
            if (!_zoomToFit && _mediaPlayer.NormalizedSourceRect == defaultSize) return;
            if (_zoomToFit)
            {
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
            else
            {
                _mediaPlayer.NormalizedSourceRect = defaultSize;
            }
        }
    }
}

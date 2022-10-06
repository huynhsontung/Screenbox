#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Media;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using LibVLCSharp.Platforms.UWP;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Converters;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core.Playback;
using CommunityToolkit.Diagnostics;
using Windows.Media.Playback;
using Windows.System.Display;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Screenbox.ViewModels
{
    internal sealed partial class PlayerElementViewModel : ObservableRecipient, IRecipient<ChangeZoomToFitMessage>
    {
        public MediaPlayer? VlcPlayer { get; private set; }

        //[ObservableProperty] private double _viewOpacity;

        private readonly LibVlcService _libVlcService;
        private readonly IWindowService _windowService;
        private readonly ISystemMediaTransportControlsService _transportControlsService;
        private readonly DispatcherQueue _dispatcherQueue;
        private Size _viewSize;
        private bool _zoomToFit;
        private VlcMediaPlayer? _mediaPlayer;
        private DisplayRequest? _displayRequest;

        public PlayerElementViewModel(
            LibVlcService libVlcService,
            IWindowService windowService,
            ISystemMediaTransportControlsService transportControlsService)
        {
            _libVlcService = libVlcService;
            _windowService = windowService;
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

        public void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null) e.DragUIOverride.Caption = "Open";
        }

        public async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    if (items.Count == 1 && items[0] is StorageFile { FileType: ".srt" or ".ass" } file)
                    {
                        _mediaPlayer?.AddSubtitle(file);
                    }
                    else
                    {
                        Messenger.Send(new PlayFilesWithNeighborsMessage(items, null));
                    }

                    return;
                }
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                Uri? uri = await e.DataView.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    Messenger.Send(new PlayMediaMessage(uri));
                }
            }
        }

        public void OnInitialized(object sender, InitializedEventArgs e)
        {
            Task.Run(() =>
            {
                _libVlcService.Initialize(e.SwapChainOptions);
                _mediaPlayer = _libVlcService.MediaPlayer;
                Guard.IsNotNull(_mediaPlayer, nameof(_mediaPlayer));
                VlcPlayer = _mediaPlayer.VlcPlayer;
                _mediaPlayer.NaturalVideoSizeChanged += OnVideoSizeChanged;
                _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
                _mediaPlayer.PositionChanged += OnPositionChanged;
                _mediaPlayer.MediaFailed += OnMediaFailed;
                Messenger.Send(new MediaPlayerChangedMessage(_mediaPlayer));
            });
        }

        private void OnMediaFailed(IMediaPlayer sender, object? args)
        {
            _transportControlsService.ClosePlayback();
        }

        private void OnPositionChanged(IMediaPlayer sender, object? args)
        {
            _transportControlsService.UpdatePlaybackPosition(sender.Position, TimeSpan.Zero, sender.NaturalDuration);
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            Messenger.Send(new ChangeVolumeMessage(mouseWheelDelta / 25, true));
        }

        public void ProcessKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_mediaPlayer == null) return;
            args.Handled = true;
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;
            VirtualKey key = sender.Key;

            switch (key)
            {
                case VirtualKey.Space:
                    switch (_mediaPlayer.PlaybackState)
                    {
                        case MediaPlaybackState.Playing:
                            _mediaPlayer.Pause();
                            break;
                        case MediaPlaybackState.Paused or MediaPlaybackState.None:
                            _mediaPlayer.Play();
                            break;
                    }
                    return;
                case VirtualKey.Left:
                case VirtualKey.J:
                    direction = -1;
                    break;
                case VirtualKey.Right:
                case VirtualKey.L:
                    direction = 1;
                    break;
                case VirtualKey.Up:
                    volumeChange = 5;
                    break;
                case VirtualKey.Down:
                    volumeChange = -5;
                    break;
                case VirtualKey.NumberPad0:
                case VirtualKey.NumberPad1:
                case VirtualKey.NumberPad2:
                case VirtualKey.NumberPad3:
                case VirtualKey.NumberPad4:
                case VirtualKey.NumberPad5:
                case VirtualKey.NumberPad6:
                case VirtualKey.NumberPad7:
                case VirtualKey.NumberPad8:
                case VirtualKey.NumberPad9:
                    _mediaPlayer.Position = (_mediaPlayer?.NaturalDuration ?? default) * (0.1 * (key - VirtualKey.NumberPad0));
                    break;
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
                case (VirtualKey)190:   // Period (".")
                    JumpFrame(false);
                    return;
                case (VirtualKey)188:   // Comma (",")
                    JumpFrame(true);
                    return;
            }

            switch (sender.Modifiers)
            {
                case VirtualKeyModifiers.Control:
                    seekAmount = 10000;
                    break;
                case VirtualKeyModifiers.Shift:
                    seekAmount = 1000;
                    break;
                case VirtualKeyModifiers.None:
                    seekAmount = 5000;
                    break;
            }

            if (seekAmount * direction != 0)
            {
                Seek(seekAmount * direction);
            }

            if (volumeChange != 0)
            {
                Messenger.Send(new ChangeVolumeMessage(volumeChange, true));
            }
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

        private void Seek(long amount)
        {
            if (_mediaPlayer?.CanSeek ?? false)
            {
                _mediaPlayer.Position += TimeSpan.FromMilliseconds(amount);
                Messenger.Send(new UpdateStatusMessage(
                    $"{HumanizedDurationConverter.Convert(_mediaPlayer.Position)} / {HumanizedDurationConverter.Convert(_mediaPlayer.NaturalDuration)}"));
            }
        }

        private bool JumpFrame(bool previous = false)
        {
            if ((_mediaPlayer?.CanSeek ?? false) && _mediaPlayer.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused)
            {
                if (previous)
                {
                    _mediaPlayer.StepBackwardOneFrame();
                }
                else
                {
                    _mediaPlayer.StepForwardOneFrame();
                }

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

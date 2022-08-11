#nullable enable

using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
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

namespace Screenbox.ViewModels
{
    internal partial class PlayerElementViewModel : ObservableRecipient, IRecipient<ChangeZoomToFitMessage>
    {
        internal VlcMediaPlayer? MediaPlayer => _libVlcService.MediaPlayer;

        //[ObservableProperty] private double _viewOpacity;

        private readonly LibVlcService _libVlcService;
        private readonly IWindowService _windowService;
        private readonly DispatcherQueue _dispatcherQueue;
        private Size _viewSize;
        private bool _zoomToFit;
        private DisplayRequest? _displayRequest;

        public PlayerElementViewModel(
            LibVlcService libVlcService,
            IWindowService windowService)
        {
            _libVlcService = libVlcService;
            _windowService = windowService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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
                        MediaPlayer?.AddSubtitle(file);
                    }
                    else
                    {
                        Play(items);
                    }

                    return;
                }
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                Uri? uri = await e.DataView.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    Play(uri);
                    return;
                }
            }
        }

        public void OnInitialized(object sender, InitializedEventArgs e)
        {
            _libVlcService.Initialize(e.SwapChainOptions);
            Guard.IsNotNull(MediaPlayer, nameof(MediaPlayer));
            MediaPlayer.NaturalVideoSizeChanged += OnVideoSizeChanged;
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            Messenger.Send(new MediaPlayerChangedMessage(MediaPlayer));
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            Messenger.Send(new ChangeVolumeMessage(mouseWheelDelta / 25, true));
        }

        public void ProcessKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (MediaPlayer == null) return;
            args.Handled = true;
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;
            VirtualKey key = sender.Key;

            switch (key)
            {
                case VirtualKey.Space:
                    switch (MediaPlayer.PlaybackState)
                    {
                        case MediaPlaybackState.Playing:
                            MediaPlayer.Pause();
                            break;
                        case MediaPlaybackState.Paused or MediaPlaybackState.None:
                            MediaPlayer.Play();
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
                    MediaPlayer.Position = (MediaPlayer?.NaturalDuration ?? default) * (0.1 * (key - VirtualKey.NumberPad0));
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

        private void Play(object? value)
        {
            if (value == null) return;
            Messenger.Send(new PlayMediaMessage(value));
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
        {
            if (sender.NaturalVideoHeight > 0 &&
                sender.PlaybackState == MediaPlaybackState.Playing &&
                _displayRequest == null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
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
                    _displayRequest.RequestRelease();
                    _displayRequest = null;
                });
            }
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
            if (MediaPlayer == null || scalar < 0 || _windowService.ViewMode != WindowViewMode.Default) return false;
            Size videoDimension = new(MediaPlayer.NaturalVideoWidth, MediaPlayer.NaturalVideoHeight);
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
            if (MediaPlayer?.CanSeek ?? false)
            {
                MediaPlayer.Position += TimeSpan.FromMilliseconds(amount);
                Messenger.Send(new UpdateStatusMessage(
                    $"{HumanizedDurationConverter.Convert(MediaPlayer.Position)} / {HumanizedDurationConverter.Convert(MediaPlayer.NaturalDuration)}"));
            }
        }

        private bool JumpFrame(bool previous = false)
        {
            if ((MediaPlayer?.CanSeek ?? false) && MediaPlayer.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused)
            {
                if (previous)
                {
                    MediaPlayer.StepBackwardOneFrame();
                }
                else
                {
                    MediaPlayer.StepForwardOneFrame();
                }

                return true;
            }

            return false;
        }

        private void SetCropGeometry(Size size)
        {
            if (MediaPlayer == null) return;
            Rect defaultSize = new Rect(0, 0, 1, 1);
            if (!_zoomToFit && MediaPlayer.NormalizedSourceRect == defaultSize) return;
            if (_zoomToFit)
            {
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
            else
            {
                MediaPlayer.NormalizedSourceRect = defaultSize;
            }
        }
    }
}

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
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Converters;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class VideoViewViewModel : ObservableRecipient
    {
        public MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private LibVLC? LibVlc => _mediaPlayerService.LibVlc;

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly IWindowService _windowService;
        private readonly INotificationService _notificationService;
        private readonly DispatcherQueue _dispatcherQueue;

        public VideoViewViewModel(
            IMediaPlayerService mediaPlayerService,
            IWindowService windowService,
            INotificationService notificationService)
        {
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.VlcPlayerChanged += OnVlcPlayerChanged;
            _windowService = windowService;
            _notificationService = notificationService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // View model does not receive any message
            //IsActive = true;
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
                    Play(items);
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
            _mediaPlayerService.InitVlcPlayer(e.SwapChainOptions);
            if (LibVlc != null) _notificationService.SetVlcDialogHandlers(LibVlc);
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            _mediaPlayerService.Volume += mouseWheelDelta / 25;
        }

        public void ProcessKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;
            VirtualKey key = sender.Key;

            switch (key)
            {
                case VirtualKey.Space:
                    PlayPause();
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
                    volumeChange = 10;
                    break;
                case VirtualKey.Down:
                    volumeChange = -10;
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
                    _mediaPlayerService.SetTime((VlcPlayer?.Length ?? 0) * (0.1 * (key - VirtualKey.NumberPad0)));
                    break;
                case VirtualKey.Number0:
                case VirtualKey.Number1:
                case VirtualKey.Number2:
                case VirtualKey.Number3:
                case VirtualKey.Number4:
                case VirtualKey.Number5:
                case VirtualKey.Number6:
                case VirtualKey.Number7:
                case VirtualKey.Number8:
                    ResizeWindow(0.25 * (key - VirtualKey.Number0));
                    return;
                case VirtualKey.Number9:
                    ResizeWindow(4);
                    return;
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
                _mediaPlayerService.Volume += volumeChange;
            }
        }

        [ICommand]
        private void PlayPause()
        {
            if (VlcPlayer?.State == VLCState.Ended)
            {
                _mediaPlayerService.Replay();
                return;
            }

            _mediaPlayerService.Pause();
        }

        private void OnVlcPlayerChanged(object sender, EventArgs e)
        {
            if (VlcPlayer != null)
            {
                VlcPlayer.MediaChanged += OnMediaChanged;
            }
        }

        private void OnMediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            e.Media.ParsedChanged += OnMediaParsed;
        }

        private void Play(object? value)
        {
            if (value == null) return;
            Messenger.Send(new PlayMediaMessage(value));
        }

        private void OnMediaParsed(object sender, MediaParsedChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ResizeWindow(1)) return;
                ResizeWindow();
            });
        }

        private bool ResizeWindow(double scalar = 0)
        {
            if (scalar < 0 || _windowService.IsCompact) return false;
            Size videoDimension = _mediaPlayerService.Dimension;
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
            if (VlcPlayer?.IsSeekable ?? false)
            {
                if (VlcPlayer.State == VLCState.Ended && amount > 0) return;
                _mediaPlayerService.Seek(amount);
                Messenger.Send(new UpdateStatusMessage(
                    $"{HumanizedDurationConverter.Convert(VlcPlayer.Time)} / {HumanizedDurationConverter.Convert(VlcPlayer.Length)}"));
            }
        }

        private bool JumpFrame(bool previous = false)
        {
            if ((VlcPlayer?.IsSeekable ?? false) && VlcPlayer.State == VLCState.Paused)
            {
                if (previous)
                {
                    _mediaPlayerService.Seek(-_mediaPlayerService.FrameDuration);
                }
                else
                {
                    _mediaPlayerService.NextFrame();
                }

                return true;
            }

            return false;
        }
    }
}

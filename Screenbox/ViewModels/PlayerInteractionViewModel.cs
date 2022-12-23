using System;
using System.Collections.Generic;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Screenbox.Converters;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Screenbox.Controls;
using Screenbox.Services;
using Screenbox.Strings;

namespace Screenbox.ViewModels
{
    internal sealed class PlayerInteractionViewModel : ObservableRecipient,
        IRecipient<SettingsChangedMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        private IMediaPlayer? _mediaPlayer;
        private ManipulationLock _manipulationLock;
        private TimeSpan _timeBeforeManipulation;
        private bool _playerSeekGesture;
        private bool _playerVolumeGesture;
        private bool _manipulationCompleted;

        private readonly ISettingsService _settingsService;

        public PlayerInteractionViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            UpdateSettings();
            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
        }

        public void Receive(SettingsChangedMessage message)
        {
            UpdateSettings();
        }

        public void OnClick()
        {
            // Manipulation will trigger click event
            if (_manipulationCompleted)
            {
                _manipulationCompleted = false;
                return;
            }

            if (_settingsService.PlayerTapGesture)
            {
                TogglePlayPause();
            }
        }

        public void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null) e.DragUIOverride.Caption = Resources.Open;
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

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            int volume = Messenger.Send(new ChangeVolumeRequestMessage(mouseWheelDelta / 25, true));
            Messenger.Send(new UpdateStatusMessage(Resources.VolumeChangeStatusMessage(volume)));
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
                case VirtualKey.K when sender.Modifiers == VirtualKeyModifiers.None:
                case VirtualKey.P when sender.Modifiers == VirtualKeyModifiers.None:
                case VirtualKey.Space:
                    TogglePlayPause();
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
                case (VirtualKey)190:   // Period (".")
                    JumpFrame(false);
                    return;
                case (VirtualKey)188:   // Comma (",")
                    JumpFrame(true);
                    return;
                default:
                    args.Handled = false;
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
                int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
                Messenger.Send(new UpdateStatusMessage(Resources.VolumeChangeStatusMessage(volume)));
            }
        }

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_manipulationLock == ManipulationLock.None) return;
            _manipulationCompleted = true;
            Messenger.Send(new OverrideControlsHideMessage(100));
            Messenger.Send(new UpdateStatusMessage(null));
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
                Messenger.Send(new UpdateStatusMessage(Resources.VolumeChangeStatusMessage(volume), true));
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
                TimeSpan newTime = Messenger.Send(new ChangeTimeRequestMessage(timeChange, true));

                string changeText = HumanizedDurationConverter.Convert(newTime - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                string status = $"{HumanizedDurationConverter.Convert(newTime)} ({changeText})";
                Messenger.Send(new UpdateStatusMessage(status, true));
            }
        }

        public void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _manipulationLock = ManipulationLock.None;
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

        private void TogglePlayPause()
        {
            if (_mediaPlayer?.PlaybackItem == null) return;
            switch (_mediaPlayer.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    _mediaPlayer.Pause();
                    break;
                case MediaPlaybackState.Paused or MediaPlaybackState.None:
                    _mediaPlayer.Play();
                    break;
            }

            Messenger.Send(new ShowPlayPauseBadgeMessage());
        }

        private void UpdateSettings()
        {
            _playerSeekGesture = _settingsService.PlayerSeekGesture;
            _playerVolumeGesture = _settingsService.PlayerVolumeGesture;
        }
    }
}

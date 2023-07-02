#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Screenbox.Core.ViewModels
{
    public sealed class PlayerInteractionViewModel : ObservableRecipient,
        IRecipient<SettingsChangedMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        private IMediaPlayer? _mediaPlayer;
        private ManipulationLock _manipulationLock;
        private TimeSpan _timeBeforeManipulation;
        private bool _playerSeekGesture;
        private bool _playerVolumeGesture;

        private readonly ISettingsService _settingsService;
        private readonly DispatcherQueueTimer _seekOriginTimer;

        public PlayerInteractionViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _seekOriginTimer = dispatcherQueue.CreateTimer();
            _seekOriginTimer.IsRepeating = false;

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

        public async void OnDrop(object sender, DragEventArgs e)
        {
            if (_mediaPlayer == null) return;
            try
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        if (items.Count == 1 && items[0] is StorageFile { FileType: ".srt" or ".ass" } file)
                        {
                            _mediaPlayer.AddSubtitle(file);
                            Messenger.Send(new SubtitleAddedNotificationMessage(file));
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
            catch (Exception exception)
            {
                Messenger.Send(new MediaLoadFailedNotificationMessage(exception.Message, string.Empty));
            }
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            int volume = Messenger.Send(new ChangeVolumeRequestMessage(mouseWheelDelta > 0 ? 5 : -5, true));
            Messenger.Send(new UpdateVolumeStatusMessage(volume, false));
        }

        public void ProcessGamepadKeyDown(object sender, KeyRoutedEventArgs args)
        {
            PlayerVisibilityState playerVisibility = Messenger.Send<PlayerVisibilityRequestMessage>();
            bool playerActive = _mediaPlayer is
            {
                Source: not null,
                PlaybackState: MediaPlaybackState.Paused
                or MediaPlaybackState.Playing
                or MediaPlaybackState.Buffering
            };

            if (!playerActive) return;
            bool handled = true;
            int volumeChange = 0;
            switch (args.Key)
            {
                case VirtualKey.GamepadRightThumbstickLeft:
                case VirtualKey.GamepadLeftShoulder:
                    Seek(-5000);
                    break;
                case VirtualKey.GamepadRightThumbstickRight:
                case VirtualKey.GamepadRightShoulder:
                    Seek(5000);
                    break;
                case VirtualKey.GamepadLeftTrigger when playerVisibility == PlayerVisibilityState.Visible:
                    Seek(-30_000);
                    break;
                case VirtualKey.GamepadRightTrigger when playerVisibility == PlayerVisibilityState.Visible:
                    Seek(30_000);
                    break;
                case VirtualKey.GamepadRightThumbstickUp:
                    volumeChange = 2;
                    break;
                case VirtualKey.GamepadRightThumbstickDown:
                    volumeChange = -2;
                    break;
                case VirtualKey.GamepadX:
                    Messenger.Send(new TogglePlayPauseMessage(true));
                    break;
                case VirtualKey.GamepadView:
                    Messenger.Send(new TogglePlayerVisibilityMessage());
                    break;
                default:
                    handled = false;
                    break;
            }

            if (volumeChange != 0)
            {
                int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volume, false));
            }

            args.Handled = handled;
        }

        public void ProcessKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            PlayerVisibilityState playerVisibility = Messenger.Send(new PlayerVisibilityRequestMessage());
            if (_mediaPlayer == null || playerVisibility != PlayerVisibilityState.Visible) return;
            args.Handled = true;
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;
            VirtualKey key = sender.Key;

            switch (key)
            {
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
                    int percent = (key - VirtualKey.NumberPad0) * 10;
                    TimeSpan newPosition = _mediaPlayer.NaturalDuration * (0.01 * percent);
                    newPosition = Messenger.Send(new ChangeTimeRequestMessage(newPosition));
                    string updateText =
                        $"{Humanizer.ToDuration(newPosition)} / {Humanizer.ToDuration(_mediaPlayer.NaturalDuration)} ({percent}%)";
                    Messenger.Send(new UpdateStatusMessage(updateText));
                    break;
                case (VirtualKey)190 when sender.Modifiers == VirtualKeyModifiers.Shift:
                    TogglePlaybackRate(true);
                    return;
                case (VirtualKey)188 when sender.Modifiers == VirtualKeyModifiers.Shift:
                    TogglePlaybackRate(false);
                    return;
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
                Messenger.Send(new UpdateVolumeStatusMessage(volume, false));
            }
        }

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_manipulationLock == ManipulationLock.None) return;
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
                Messenger.Send(new UpdateVolumeStatusMessage(volume, true));
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

                string changeText = Humanizer.ToDuration(newTime - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                string status = $"{Humanizer.ToDuration(newTime)} ({changeText})";
                Messenger.Send(new UpdateStatusMessage(status, true));
            }
        }

        public void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _manipulationLock = ManipulationLock.None;
        }

        private void TogglePlaybackRate(bool speedUp)
        {
            if (_mediaPlayer == null) return;
            Span<double> steps = stackalloc[] { 0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2 };
            double lastPositiveStep = steps[0];
            foreach (double step in steps)
            {
                double diff = step - _mediaPlayer.PlaybackRate;
                if (speedUp && diff > 0)
                {
                    _mediaPlayer.PlaybackRate = step;
                    Messenger.Send(new UpdateStatusMessage($"{step}×"));
                    return;
                }

                if (!speedUp)
                {
                    if (-diff > 0)
                    {
                        lastPositiveStep = step;
                    }
                    else
                    {
                        _mediaPlayer.PlaybackRate = lastPositiveStep;
                        Messenger.Send(new UpdateStatusMessage($"{lastPositiveStep}×"));
                        return;
                    }
                }
            }
        }

        private void Seek(long amount)
        {
            if (_mediaPlayer?.CanSeek ?? false)
            {
                _seekOriginTimer.Debounce(() => _timeBeforeManipulation = _mediaPlayer.Position, TimeSpan.FromSeconds(1), true);
                TimeSpan newPosition = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromMilliseconds(amount), true, false));

                double persistentOffset = (newPosition - _timeBeforeManipulation).TotalMilliseconds;
                Messenger.Send(new UpdateStatusMessage(
                    $"{Humanizer.ToDuration(newPosition)} / {Humanizer.ToDuration(_mediaPlayer.NaturalDuration)} ({(persistentOffset > 0 ? '+' : string.Empty)}{Humanizer.ToDuration(persistentOffset)})"));
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

        private void UpdateSettings()
        {
            _playerSeekGesture = _settingsService.PlayerSeekGesture;
            _playerVolumeGesture = _settingsService.PlayerVolumeGesture;
        }
    }
}

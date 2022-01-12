using Windows.Media;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel
    {
        private SystemMediaTransportControlsDisplayUpdater InitSystemTransportControls()
        {
            TransportControl.IsEnabled = true;
            TransportControl.IsPlayEnabled = true;
            TransportControl.IsPauseEnabled = true;
            TransportControl.IsStopEnabled = true;
            TransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;

            var updater = TransportControl.DisplayUpdater;
            updater.ClearAll();
            updater.AppMediaId = "Modern VLC";
            return updater;
        }

        // TODO: Update SystemMediaTransportControlsDisplayUpdater with media metadata

        private void RegisterMediaPlayerPlaybackEvents()
        {
            MediaPlayer.Paused += (sender, args) => DispatcherQueue.TryEnqueue(() => TransportControl.PlaybackStatus = MediaPlaybackStatus.Paused);
            MediaPlayer.Stopped += (sender, args) => DispatcherQueue.TryEnqueue(() => TransportControl.PlaybackStatus = MediaPlaybackStatus.Stopped);
            MediaPlayer.Playing += (sender, args) => DispatcherQueue.TryEnqueue(() => TransportControl.PlaybackStatus = MediaPlaybackStatus.Playing);
            MediaPlayer.EncounteredError += (sender, args) => DispatcherQueue.TryEnqueue(() => TransportControl.PlaybackStatus = MediaPlaybackStatus.Closed);
            MediaPlayer.Opening += (sender, args) => DispatcherQueue.TryEnqueue(() => TransportControl.PlaybackStatus = MediaPlaybackStatus.Changing);
        }

        private void TransportControl_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    MediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    MediaPlayer.Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    MediaPlayer.Stop();
                    break;
                    //case SystemMediaTransportControlsButton.Previous:
                    //    Locator.PlaybackService.Previous();
                    //    break;
                    //case SystemMediaTransportControlsButton.Next:
                    //    Locator.PlaybackService.Next();
                    //    break;
                    //case SystemMediaTransportControlsButton.FastForward:
                    //    FastSeekCommand.Execute(30000);
                    //    break;
                    //case SystemMediaTransportControlsButton.Rewind:
                    //    FastSeekCommand.Execute(-30000);
                    //    break;
            }
        }
    }
}

using Windows.Media;

namespace Screenbox.ViewModels
{
    internal partial class PlayerViewModel
    {
        private SystemMediaTransportControlsDisplayUpdater InitSystemTransportControls()
        {
            _transportControl.IsEnabled = true;
            _transportControl.IsPlayEnabled = true;
            _transportControl.IsPauseEnabled = true;
            _transportControl.IsStopEnabled = true;
            _transportControl.PlaybackStatus = MediaPlaybackStatus.Playing;

            var updater = _transportControl.DisplayUpdater;
            updater.ClearAll();
            updater.AppMediaId = "Modern VLC";
            return updater;
        }

        // TODO: Update SystemMediaTransportControlsDisplayUpdater with media metadata

        private void RegisterMediaPlayerPlaybackEvents()
        {
            MediaPlayer.Paused += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Paused);
            MediaPlayer.Stopped += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Stopped);
            MediaPlayer.Playing += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Playing);
            MediaPlayer.EncounteredError += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Closed);
            MediaPlayer.Opening += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Changing);
        }

        private void TransportControl_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (MediaPlayer == null) return;
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

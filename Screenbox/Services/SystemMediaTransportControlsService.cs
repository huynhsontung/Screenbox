#nullable enable

using Windows.Foundation;
using Windows.Media;
using Windows.System;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal class SystemMediaTransportControlsService : ISystemMediaTransportControlsService
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly SystemMediaTransportControls _transportControl;
        private readonly SystemMediaTransportControlsDisplayUpdater _displayUpdater;

        private ObservablePlayer? _player;

        public SystemMediaTransportControlsService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _transportControl = SystemMediaTransportControls.GetForCurrentView();

            _transportControl.ButtonPressed += TransportControl_ButtonPressed;
            _transportControl.IsEnabled = true;
            _transportControl.IsPlayEnabled = true;
            _transportControl.IsPauseEnabled = true;
            _transportControl.IsStopEnabled = true;
            _transportControl.PlaybackStatus = MediaPlaybackStatus.Closed;

            _displayUpdater = _transportControl.DisplayUpdater;
            _displayUpdater.ClearAll();
            _displayUpdater.AppMediaId = "Modern VLC";
        }

        public void RegisterPlaybackEvents(ObservablePlayer player)
        {
            _player = player;
            player.VlcPlayer.Paused += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Paused);
            player.VlcPlayer.Stopped += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Stopped);
            player.VlcPlayer.Playing += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Playing);
            player.VlcPlayer.EncounteredError += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Closed);
            player.VlcPlayer.Opening += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Changing);
        }

        private void TransportControl_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (_player == null) return;
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    _player.Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    _player.Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    _player.Stop();
                    break;
                //case SystemMediaTransportControlsButton.Previous:
                //    Locator.PlaybackService.Previous();
                //    break;
                //case SystemMediaTransportControlsButton.Next:
                //    Locator.PlaybackService.Next();
                //    break;
                case SystemMediaTransportControlsButton.FastForward:
                    _player.Seek(30000);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    _player.Seek(-30000);
                    break;
            }
        }
    }
}

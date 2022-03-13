#nullable enable

using Windows.Foundation;
using Windows.Media;
using Windows.System;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal class SystemMediaTransportControlsService : ISystemMediaTransportControlsService
    {
        public event TypedEventHandler<SystemMediaTransportControls, SystemMediaTransportControlsButtonPressedEventArgs>? ButtonPressed;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly SystemMediaTransportControls _transportControl;
        private readonly SystemMediaTransportControlsDisplayUpdater _displayUpdater;

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
            player.VlcPlayer.Paused += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Paused);
            player.VlcPlayer.Stopped += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Stopped);
            player.VlcPlayer.Playing += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Playing);
            player.VlcPlayer.EncounteredError += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Closed);
            player.VlcPlayer.Opening += (sender, args) => _dispatcherQueue.TryEnqueue(() => _transportControl.PlaybackStatus = MediaPlaybackStatus.Changing);
        }

        private void TransportControl_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            ButtonPressed?.Invoke(sender, args);
        }
    }
}

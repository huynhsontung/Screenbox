using Windows.Foundation;
using Windows.Media;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface ISystemMediaTransportControlsService
    {
        event TypedEventHandler<SystemMediaTransportControls, SystemMediaTransportControlsButtonPressedEventArgs> ButtonPressed;
        void RegisterPlaybackEvents(ObservablePlayer player);
    }
}
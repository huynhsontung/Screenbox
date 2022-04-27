using Windows.Foundation;
using Windows.Media;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface ISystemMediaTransportControlsService
    {
        void RegisterPlaybackEvents(ObservablePlayer player);
    }
}
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal interface ISystemMediaTransportControlsService
    {
        void RegisterPlaybackEvents(ObservablePlayer player);
    }
}
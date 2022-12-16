using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface ISettingsService
    {
        PlayerAutoResizeOptions PlayerAutoResize { get; set; }
        bool PlayerVolumeGesture { get; set; }
        bool PlayerSeekGesture { get; set; }
    }
}
using Screenbox.Core;

namespace Screenbox.Core.Services
{
    public interface ISettingsService
    {
        PlayerAutoResizeOption PlayerAutoResize { get; set; }
        bool PlayerVolumeGesture { get; set; }
        bool PlayerSeekGesture { get; set; }
        bool PlayerTapGesture { get; set; }
        int PersistentVolume { get; set; }
        bool ShowRecent { get; set; }
        int MaxVolume { get; set; }
    }
}
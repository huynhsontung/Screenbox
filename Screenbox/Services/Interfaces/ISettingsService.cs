using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface ISettingsService
    {
        PlayerAutoResizeOptions PlayerAutoResize { get; set; }
        bool PlayerVolumeGesture { get; set; }
        bool PlayerSeekGesture { get; set; }
        bool PlayerTapGesture { get; set; }
        int PersistentVolume { get; set; }
        bool ShowVideoFolders { get; set; }
    }
}
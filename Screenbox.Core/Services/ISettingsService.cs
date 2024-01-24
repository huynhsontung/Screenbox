using Screenbox.Core.Enums;
using Windows.Media;

namespace Screenbox.Core.Services
{
    public interface ISettingsService
    {
        PlayerAutoResizeOption PlayerAutoResize { get; set; }
        bool UseIndexer { get; set; }
        bool PlayerVolumeGesture { get; set; }
        bool PlayerSeekGesture { get; set; }
        bool PlayerTapGesture { get; set; }
        int PersistentVolume { get; set; }
        bool ShowRecent { get; set; }
        bool SearchRemovableStorage { get; set; }
        int MaxVolume { get; set; }
        string GlobalArguments { get; set; }
        bool AdvancedMode { get; set; }
        MediaPlaybackAutoRepeatMode PersistentRepeatMode { get; set; }
    }
}
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
        bool PlayerShowControls { get; set; }
        bool PlayerShowChapters { get; set; }
        int PlayerControlsHideDelay { get; set; }
        int PersistentVolume { get; set; }
        string PersistentSubtitleLanguage { get; set; }
        bool ShowRecent { get; set; }
        ThemeOption Theme { get; set; }
        bool EnqueueAllFilesInFolder { get; set; }
        bool RestorePlaybackPosition { get; set; }
        bool TrackLastPosition { get; set; }
        bool SearchRemovableStorage { get; set; }
        int MaxVolume { get; set; }
        string GlobalArguments { get; set; }
        bool AdvancedMode { get; set; }
        VideoUpscaleOption VideoUpscale { get; set; }
        bool UseMultipleInstances { get; set; }
        string LivelyActivePath { get; set; }
        MediaPlaybackAutoRepeatMode PersistentRepeatMode { get; set; }
    }
}

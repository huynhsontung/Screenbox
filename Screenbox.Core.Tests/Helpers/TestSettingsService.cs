using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Windows.Media;

namespace Screenbox.Core.Tests.Helpers;

public class TestSettingsService : ISettingsService
{
    public PlayerAutoResizeOption PlayerAutoResize { get; set; } = PlayerAutoResizeOption.Never;
    public bool UseIndexer { get; set; } = true;
    public bool PlayerShowControls { get; set; } = true;
    public bool PersistentShowRemainingTime { get; set; }
    public bool PlayerShowChapters { get; set; } = true;
    public int PlayerControlsHideDelay { get; set; } = 3;
    public int PersistentVolume { get; set; } = 100;
    public string PersistentSubtitleLanguage { get; set; } = string.Empty;
    public bool ShowRecent { get; set; } = true;
    public ThemeOption Theme { get; set; } = ThemeOption.Auto;
    public bool EnqueueAllFilesInFolder { get; set; }
    public bool RestorePlaybackPosition { get; set; }
    public bool SearchRemovableStorage { get; set; } = true;
    public int MaxVolume { get; set; } = 100;
    public string GlobalArguments { get; set; } = string.Empty;
    public bool AdvancedMode { get; set; }
    public VideoUpscaleOption VideoUpscale { get; set; } = VideoUpscaleOption.Linear;
    public bool UseMultipleInstances { get; set; }
    public string LivelyActivePath { get; set; } = string.Empty;
    public MediaPlaybackAutoRepeatMode PersistentRepeatMode { get; set; } = MediaPlaybackAutoRepeatMode.None;
    public string FrameCaptureFolderToken { get; set; } = string.Empty;
    public bool PersistPlaybackPosition { get; set; } = true;
    public int PlayerRewindStep { get; set; } = 5;
    public int PlayerFastForwardStep { get; set; } = 5;
    public PlaybackActionKind PlayerGestureTap { get; set; } = PlaybackActionKind.PlayPause;
    public PlaybackActionKind PlayerGestureSwipeUp { get; set; } = PlaybackActionKind.IncreaseVolume;
    public PlaybackActionKind PlayerGestureSwipeDown { get; set; } = PlaybackActionKind.DecreaseVolume;
    public PlaybackActionKind PlayerGestureSwipeLeft { get; set; } = PlaybackActionKind.Rewind;
    public PlaybackActionKind PlayerGestureSwipeRight { get; set; } = PlaybackActionKind.FastForward;
    public bool PlayerGestureSlideVertical { get; set; } = true;
    public bool PlayerGestureSlideHorizontal { get; set; } = true;
    public bool PlayerGesturePressAndHold { get; set; } = true;
    public SongSortOrder SongsSortOrder { get; set; } = SongSortOrder.Title;
    public AlbumSortOrder AlbumsSortOrder { get; set; } = AlbumSortOrder.Title;
}

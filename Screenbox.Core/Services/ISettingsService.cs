using Screenbox.Core.Enums;
using Windows.Media;

namespace Screenbox.Core.Services;

public interface ISettingsService
{
    PlayerAutoResizeOption PlayerAutoResize { get; set; }
    bool UseIndexer { get; set; }
    bool PlayerShowControls { get; set; }
    bool PlayerShowChapters { get; set; }
    int PlayerControlsHideDelay { get; set; }
    int PersistentVolume { get; set; }
    string PersistentSubtitleLanguage { get; set; }
    bool ShowRecent { get; set; }
    ThemeOption Theme { get; set; }
    bool EnqueueAllFilesInFolder { get; set; }
    bool RestorePlaybackPosition { get; set; }
    bool SearchRemovableStorage { get; set; }
    int MaxVolume { get; set; }
    string GlobalArguments { get; set; }
    bool AdvancedMode { get; set; }
    VideoUpscaleOption VideoUpscale { get; set; }
    bool UseMultipleInstances { get; set; }
    string LivelyActivePath { get; set; }
    MediaPlaybackAutoRepeatMode PersistentRepeatMode { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the playback position should be saved
    /// and restored between sessions.
    /// </summary>
    bool PersistPlaybackPosition { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by a tap gesture.
    /// </summary>
    PlaybackActionKind PlayerGestureTap { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by an upward swipe gesture.
    /// </summary>
    PlaybackActionKind PlayerGestureSwipeUp { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by a downward swipe gesture.
    /// </summary>
    PlaybackActionKind PlayerGestureSwipeDown { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by a left swipe gesture.
    /// </summary>
    PlaybackActionKind PlayerGestureSwipeLeft { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by a right swipe gesture.
    /// </summary>
    PlaybackActionKind PlayerGestureSwipeRight { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the press and hold gesture
    /// is enabled in the player.
    /// </summary>
    bool PlayerGesturePressAndHold { get; set; }
}

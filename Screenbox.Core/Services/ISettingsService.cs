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
    /// <value>
    /// A value of the enumeration that specifies the media command invoked by
    /// a tap gesture.
    /// </value>
    PlaybackActionKind PlayerGestureTap { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by an upward swipe gesture.
    /// </summary>
    /// <value>
    /// A value of the enumeration that specifies the media command invoked
    /// by an upward swipe gesture.
    /// </value>
    PlaybackActionKind PlayerGestureSwipeUp { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by a downward swipe gesture.
    /// </summary>
    /// <value>
    /// A value of the enumeration that specifies the media command invoked by
    /// a downward swipe gesture.
    /// </value>
    PlaybackActionKind PlayerGestureSwipeDown { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by a left swipe gesture.
    /// </summary>
    /// <value>
    /// A value of the enumeration that specifies the media command invoked by
    /// a left swipe gesture.
    /// </value>
    PlaybackActionKind PlayerGestureSwipeLeft { get; set; }

    /// <summary>
    /// Gets or sets the media command invoked by a right swipe gesture.
    /// </summary>
    /// <value>
    /// A value of the enumeration that specifies the media command invoked by
    /// a right swipe gesture.
    /// </value>
    PlaybackActionKind PlayerGestureSwipeRight { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether vertical slide gestures
    /// (up/down) are enabled in the player.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if vertical slide gestures adjust playback volume;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool PlayerGestureSlideVertical { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether horizontal slide gestures
    /// (left/right) are enabled in the player.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if horizontal slide gestures seek the playback position;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool PlayerGestureSlideHorizontal { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the press and hold gesture
    /// is enabled in the player.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the press and hold gesture is enabled;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool PlayerGesturePressAndHold { get; set; }
}

using Screenbox.Core.Enums;
using Windows.Media;

namespace Screenbox.Core.Services
{
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
        /// Gets or sets the media command invoked by a tap gesture.
        /// </summary>
        PlayerGestureOption PlayerTapGesture { get; set; }

        /// <summary>
        /// Gets or sets the media command invoked by an upward swipe gesture.
        /// </summary>
        PlayerGestureOption PlayerSwipeUpGesture { get; set; }

        /// <summary>
        /// Gets or sets the media command invoked by a downward swipe gesture.
        /// </summary>
        PlayerGestureOption PlayerSwipeDownGesture { get; set; }

        /// <summary>
        /// Gets or sets the media command invoked by a left swipe gesture.
        /// </summary>
        PlayerGestureOption PlayerSwipeLeftGesture { get; set; }

        /// <summary>
        /// Gets or sets the media command invoked by a right swipe gesture.
        /// </summary>
        PlayerGestureOption PlayerSwipeRightGesture { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the tap and hold gesture
        /// is enabled in the player.
        /// </summary>
        bool PlayerTapAndHoldGesture { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the horizontal slide gesture
        /// is enabled in the player.
        /// </summary>
        bool PlayerSlideHorizontalGesture { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the vertical slide gesture
        /// is enabled in the player.
        /// </summary>
        bool PlayerSlideVerticalGesture { get; set; }
    }
}

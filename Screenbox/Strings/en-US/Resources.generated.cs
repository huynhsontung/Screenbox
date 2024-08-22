// File generated automatically by ReswPlus. https://github.com/DotNetPlus/ReswPlus
// The NuGet package ReswPlusLib is necessary to support Pluralization.
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Data;

namespace Screenbox.Strings{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("DotNetPlus.ReswPlus", "2.1.3")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public static class Resources {
        private static ResourceLoader _resourceLoader;
        static Resources()
        {
            _resourceLoader = ResourceLoader.GetForViewIndependentUse("Resources");
        }

        #region CompactOverlayToggle
        /// <summary>
        ///   Get the variant version of the string similar to: Picture in picture
        /// </summary>
        public static string CompactOverlayToggle(object variantId)
        {
            try
            {
                return CompactOverlayToggle(Convert.ToInt64(variantId));
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///   Get the variant version of the string similar to: Picture in picture
        /// </summary>
        public static string CompactOverlayToggle(long variantId)
        {
            return _resourceLoader.GetString("CompactOverlayToggle_Variant" + variantId);
        }
        #endregion

        #region MuteToggle
        /// <summary>
        ///   Get the variant version of the string similar to: Mute
        /// </summary>
        public static string MuteToggle(object variantId)
        {
            try
            {
                return MuteToggle(Convert.ToInt64(variantId));
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///   Get the variant version of the string similar to: Mute
        /// </summary>
        public static string MuteToggle(long variantId)
        {
            return _resourceLoader.GetString("MuteToggle_Variant" + variantId);
        }
        #endregion

        #region FullscreenToggle
        /// <summary>
        ///   Get the variant version of the string similar to: Fullscreen
        /// </summary>
        public static string FullscreenToggle(object variantId)
        {
            try
            {
                return FullscreenToggle(Convert.ToInt64(variantId));
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///   Get the variant version of the string similar to: Fullscreen
        /// </summary>
        public static string FullscreenToggle(long variantId)
        {
            return _resourceLoader.GetString("FullscreenToggle_Variant" + variantId);
        }
        #endregion

        #region RepeatMode
        /// <summary>
        ///   Get the variant version of the string similar to: Repeat: Off
        /// </summary>
        public static string RepeatMode(object variantId)
        {
            try
            {
                return RepeatMode(Convert.ToInt64(variantId));
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///   Get the variant version of the string similar to: Repeat: Off
        /// </summary>
        public static string RepeatMode(long variantId)
        {
            return _resourceLoader.GetString("RepeatMode_Variant" + variantId);
        }
        #endregion

        #region ItemsSelected
        /// <summary>
        ///   Get the pluralized version of the string similar to: {0} item selected
        /// </summary>
        public static string ItemsSelected(int pluralCount)
        {
            return string.Format(ReswPlusLib.ResourceLoaderExtension.GetPlural(_resourceLoader, "ItemsSelected", (double)pluralCount, false), pluralCount);
        }
        #endregion

        #region ItemsCount
        /// <summary>
        ///   Get the pluralized version of the string similar to: {0} item
        /// </summary>
        public static string ItemsCount(double pluralCount)
        {
            return string.Format(ReswPlusLib.ResourceLoaderExtension.GetPlural(_resourceLoader, "ItemsCount", pluralCount, true), pluralCount);
        }
        #endregion

        #region LocationSpecified
        /// <summary>
        ///   Get the pluralized version of the string similar to: {0} location specified
        /// </summary>
        public static string LocationSpecified(int pluralCount)
        {
            return string.Format(ReswPlusLib.ResourceLoaderExtension.GetPlural(_resourceLoader, "LocationSpecified", (double)pluralCount, false), pluralCount);
        }
        #endregion

        #region ShuffleMode
        /// <summary>
        ///   Get the variant version of the string similar to: Shuffle: Off
        /// </summary>
        public static string ShuffleMode(object variantId)
        {
            try
            {
                return ShuffleMode(Convert.ToInt64(variantId));
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///   Get the variant version of the string similar to: Shuffle: Off
        /// </summary>
        public static string ShuffleMode(long variantId)
        {
            return _resourceLoader.GetString("ShuffleMode_Variant" + variantId);
        }
        #endregion

        #region SongsCount
        /// <summary>
        ///   Get the pluralized version of the string similar to: Empty
        /// </summary>
        public static string SongsCount(int pluralCount)
        {
            return string.Format(ReswPlusLib.ResourceLoaderExtension.GetPlural(_resourceLoader, "SongsCount", (double)pluralCount, true), pluralCount);
        }
        #endregion

        #region AlbumsCount
        /// <summary>
        ///   Get the pluralized version of the string similar to: {0} album
        /// </summary>
        public static string AlbumsCount(int pluralCount)
        {
            return string.Format(ReswPlusLib.ResourceLoaderExtension.GetPlural(_resourceLoader, "AlbumsCount", (double)pluralCount, true), pluralCount);
        }
        #endregion

        #region FrameSavedNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Frame saved
        /// </summary>
        public static string FrameSavedNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("FrameSavedNotificationTitle");
            }
        }
        #endregion

        #region SaveCurrentFrame
        /// <summary>
        ///   Looks up a localized string similar to: Save current frame
        /// </summary>
        public static string SaveCurrentFrame
        {
            get
            {
                return _resourceLoader.GetString("SaveCurrentFrame");
            }
        }
        #endregion

        #region Loop
        /// <summary>
        ///   Looks up a localized string similar to: Loop
        /// </summary>
        public static string Loop
        {
            get
            {
                return _resourceLoader.GetString("Loop");
            }
        }
        #endregion

        #region PlaybackSpeed
        /// <summary>
        ///   Looks up a localized string similar to: Playback speed
        /// </summary>
        public static string PlaybackSpeed
        {
            get
            {
                return _resourceLoader.GetString("PlaybackSpeed");
            }
        }
        #endregion

        #region VolumeChangeStatusMessage
        /// <summary>
        ///   Looks up a localized string similar to: Volume {0:F0}%
        /// </summary>
        public static string VolumeChangeStatusMessage(double volume)
        {
            return string.Format(_resourceLoader.GetString("VolumeChangeStatusMessage"), volume);
        }
        #endregion

        #region FailedToSaveFrameNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Failed to save frame
        /// </summary>
        public static string FailedToSaveFrameNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("FailedToSaveFrameNotificationTitle");
            }
        }
        #endregion

        #region ChapterName
        /// <summary>
        ///   Looks up a localized string similar to: Chapter {0}
        /// </summary>
        public static string ChapterName(int chapter)
        {
            return string.Format(_resourceLoader.GetString("ChapterName"), chapter);
        }
        #endregion

        #region FailedToLoadSubtitleNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Failed to load subtitle
        /// </summary>
        public static string FailedToLoadSubtitleNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("FailedToLoadSubtitleNotificationTitle");
            }
        }
        #endregion

        #region Back
        /// <summary>
        ///   Looks up a localized string similar to: Back
        /// </summary>
        public static string Back
        {
            get
            {
                return _resourceLoader.GetString("Back");
            }
        }
        #endregion

        #region AudioAndCaption
        /// <summary>
        ///   Looks up a localized string similar to: Audio & captions
        /// </summary>
        public static string AudioAndCaption
        {
            get
            {
                return _resourceLoader.GetString("AudioAndCaption");
            }
        }
        #endregion

        #region Volume
        /// <summary>
        ///   Looks up a localized string similar to: Volume
        /// </summary>
        public static string Volume
        {
            get
            {
                return _resourceLoader.GetString("Volume");
            }
        }
        #endregion

        #region Seek
        /// <summary>
        ///   Looks up a localized string similar to: Seek
        /// </summary>
        public static string Seek
        {
            get
            {
                return _resourceLoader.GetString("Seek");
            }
        }
        #endregion

        #region Next
        /// <summary>
        ///   Looks up a localized string similar to: Next
        /// </summary>
        public static string Next
        {
            get
            {
                return _resourceLoader.GetString("Next");
            }
        }
        #endregion

        #region Previous
        /// <summary>
        ///   Looks up a localized string similar to: Previous
        /// </summary>
        public static string Previous
        {
            get
            {
                return _resourceLoader.GetString("Previous");
            }
        }
        #endregion

        #region Play
        /// <summary>
        ///   Looks up a localized string similar to: Play
        /// </summary>
        public static string Play
        {
            get
            {
                return _resourceLoader.GetString("Play");
            }
        }
        #endregion

        #region Pause
        /// <summary>
        ///   Looks up a localized string similar to: Pause
        /// </summary>
        public static string Pause
        {
            get
            {
                return _resourceLoader.GetString("Pause");
            }
        }
        #endregion

        #region More
        /// <summary>
        ///   Looks up a localized string similar to: More
        /// </summary>
        public static string More
        {
            get
            {
                return _resourceLoader.GetString("More");
            }
        }
        #endregion

        #region PlayQueue
        /// <summary>
        ///   Looks up a localized string similar to: Play queue
        /// </summary>
        public static string PlayQueue
        {
            get
            {
                return _resourceLoader.GetString("PlayQueue");
            }
        }
        #endregion

        #region AddToQueue
        /// <summary>
        ///   Looks up a localized string similar to: Add to queue
        /// </summary>
        public static string AddToQueue
        {
            get
            {
                return _resourceLoader.GetString("AddToQueue");
            }
        }
        #endregion

        #region ClearSelection
        /// <summary>
        ///   Looks up a localized string similar to: Clear selection
        /// </summary>
        public static string ClearSelection
        {
            get
            {
                return _resourceLoader.GetString("ClearSelection");
            }
        }
        #endregion

        #region Remove
        /// <summary>
        ///   Looks up a localized string similar to: Remove
        /// </summary>
        public static string Remove
        {
            get
            {
                return _resourceLoader.GetString("Remove");
            }
        }
        #endregion

        #region PlayNext
        /// <summary>
        ///   Looks up a localized string similar to: Play next
        /// </summary>
        public static string PlayNext
        {
            get
            {
                return _resourceLoader.GetString("PlayNext");
            }
        }
        #endregion

        #region MoveUp
        /// <summary>
        ///   Looks up a localized string similar to: Move up
        /// </summary>
        public static string MoveUp
        {
            get
            {
                return _resourceLoader.GetString("MoveUp");
            }
        }
        #endregion

        #region MoveDown
        /// <summary>
        ///   Looks up a localized string similar to: Move down
        /// </summary>
        public static string MoveDown
        {
            get
            {
                return _resourceLoader.GetString("MoveDown");
            }
        }
        #endregion

        #region IsPlaying
        /// <summary>
        ///   Looks up a localized string similar to: Is playing
        /// </summary>
        public static string IsPlaying
        {
            get
            {
                return _resourceLoader.GetString("IsPlaying");
            }
        }
        #endregion

        #region Videos
        /// <summary>
        ///   Looks up a localized string similar to: Videos
        /// </summary>
        public static string Videos
        {
            get
            {
                return _resourceLoader.GetString("Videos");
            }
        }
        #endregion

        #region EmptyFolder
        /// <summary>
        ///   Looks up a localized string similar to: This folder is empty
        /// </summary>
        public static string EmptyFolder
        {
            get
            {
                return _resourceLoader.GetString("EmptyFolder");
            }
        }
        #endregion

        #region RestoreView
        /// <summary>
        ///   Looks up a localized string similar to: Restore video view
        /// </summary>
        public static string RestoreView
        {
            get
            {
                return _resourceLoader.GetString("RestoreView");
            }
        }
        #endregion

        #region Cast
        /// <summary>
        ///   Looks up a localized string similar to: Cast
        /// </summary>
        public static string Cast
        {
            get
            {
                return _resourceLoader.GetString("Cast");
            }
        }
        #endregion

        #region StopCast
        /// <summary>
        ///   Looks up a localized string similar to: Stop casting
        /// </summary>
        public static string StopCast
        {
            get
            {
                return _resourceLoader.GetString("StopCast");
            }
        }
        #endregion

        #region CastingTo
        /// <summary>
        ///   Looks up a localized string similar to: Casting to
        /// </summary>
        public static string CastingTo
        {
            get
            {
                return _resourceLoader.GetString("CastingTo");
            }
        }
        #endregion

        #region CastToDevice
        /// <summary>
        ///   Looks up a localized string similar to: Cast to a device
        /// </summary>
        public static string CastToDevice
        {
            get
            {
                return _resourceLoader.GetString("CastToDevice");
            }
        }
        #endregion

        #region Disable
        /// <summary>
        ///   Looks up a localized string similar to: Disable
        /// </summary>
        public static string Disable
        {
            get
            {
                return _resourceLoader.GetString("Disable");
            }
        }
        #endregion

        #region Music
        /// <summary>
        ///   Looks up a localized string similar to: Music
        /// </summary>
        public static string Music
        {
            get
            {
                return _resourceLoader.GetString("Music");
            }
        }
        #endregion

        #region ShuffleAndPlay
        /// <summary>
        ///   Looks up a localized string similar to: Shuffle and play
        /// </summary>
        public static string ShuffleAndPlay
        {
            get
            {
                return _resourceLoader.GetString("ShuffleAndPlay");
            }
        }
        #endregion

        #region UnknownArtist
        /// <summary>
        ///   Looks up a localized string similar to: Unknown artist
        /// </summary>
        public static string UnknownArtist
        {
            get
            {
                return _resourceLoader.GetString("UnknownArtist");
            }
        }
        #endregion

        #region UnknownAlbum
        /// <summary>
        ///   Looks up a localized string similar to: Unknown album
        /// </summary>
        public static string UnknownAlbum
        {
            get
            {
                return _resourceLoader.GetString("UnknownAlbum");
            }
        }
        #endregion

        #region UnknownGenre
        /// <summary>
        ///   Looks up a localized string similar to: Unknown genre
        /// </summary>
        public static string UnknownGenre
        {
            get
            {
                return _resourceLoader.GetString("UnknownGenre");
            }
        }
        #endregion

        #region AddFolder
        /// <summary>
        ///   Looks up a localized string similar to: Add folder
        /// </summary>
        public static string AddFolder
        {
            get
            {
                return _resourceLoader.GetString("AddFolder");
            }
        }
        #endregion

        #region Clear
        /// <summary>
        ///   Looks up a localized string similar to: Clear
        /// </summary>
        public static string Clear
        {
            get
            {
                return _resourceLoader.GetString("Clear");
            }
        }
        #endregion

        #region Home
        /// <summary>
        ///   Looks up a localized string similar to: Home
        /// </summary>
        public static string Home
        {
            get
            {
                return _resourceLoader.GetString("Home");
            }
        }
        #endregion

        #region Multiselect
        /// <summary>
        ///   Looks up a localized string similar to: Select multiple
        /// </summary>
        public static string Multiselect
        {
            get
            {
                return _resourceLoader.GetString("Multiselect");
            }
        }
        #endregion

        #region NoMusicPanelHeader
        /// <summary>
        ///   Looks up a localized string similar to: We couldn't find any music
        /// </summary>
        public static string NoMusicPanelHeader
        {
            get
            {
                return _resourceLoader.GetString("NoMusicPanelHeader");
            }
        }
        #endregion

        #region NoMusicPanelSubtext
        /// <summary>
        ///   Looks up a localized string similar to: Your music library doesn't contain any music content.
        /// </summary>
        public static string NoMusicPanelSubtext
        {
            get
            {
                return _resourceLoader.GetString("NoMusicPanelSubtext");
            }
        }
        #endregion

        #region OpenFiles
        /// <summary>
        ///   Looks up a localized string similar to: Open file(s)
        /// </summary>
        public static string OpenFiles
        {
            get
            {
                return _resourceLoader.GetString("OpenFiles");
            }
        }
        #endregion

        #region SearchBoxPlaceholderText
        /// <summary>
        ///   Looks up a localized string similar to: Search
        /// </summary>
        public static string SearchBoxPlaceholderText
        {
            get
            {
                return _resourceLoader.GetString("SearchBoxPlaceholderText");
            }
        }
        #endregion

        #region Settings
        /// <summary>
        ///   Looks up a localized string similar to: Settings
        /// </summary>
        public static string Settings
        {
            get
            {
                return _resourceLoader.GetString("Settings");
            }
        }
        #endregion

        #region WelcomePanelHeader
        /// <summary>
        ///   Looks up a localized string similar to: Welcome to Screenbox
        /// </summary>
        public static string WelcomePanelHeader
        {
            get
            {
                return _resourceLoader.GetString("WelcomePanelHeader");
            }
        }
        #endregion

        #region WelcomePanelSubtext
        /// <summary>
        ///   Looks up a localized string similar to: Let's start playing some of your media content to get things going.
        /// </summary>
        public static string WelcomePanelSubtext
        {
            get
            {
                return _resourceLoader.GetString("WelcomePanelSubtext");
            }
        }
        #endregion

        #region Properties
        /// <summary>
        ///   Looks up a localized string similar to: Properties
        /// </summary>
        public static string Properties
        {
            get
            {
                return _resourceLoader.GetString("Properties");
            }
        }
        #endregion

        #region Close
        /// <summary>
        ///   Looks up a localized string similar to: Close
        /// </summary>
        public static string Close
        {
            get
            {
                return _resourceLoader.GetString("Close");
            }
        }
        #endregion

        #region OpenFileLocation
        /// <summary>
        ///   Looks up a localized string similar to: Open file location
        /// </summary>
        public static string OpenFileLocation
        {
            get
            {
                return _resourceLoader.GetString("OpenFileLocation");
            }
        }
        #endregion

        #region PropertyAlbum
        /// <summary>
        ///   Looks up a localized string similar to: Album
        /// </summary>
        public static string PropertyAlbum
        {
            get
            {
                return _resourceLoader.GetString("PropertyAlbum");
            }
        }
        #endregion

        #region PropertyAlbumArtist
        /// <summary>
        ///   Looks up a localized string similar to: Album artist
        /// </summary>
        public static string PropertyAlbumArtist
        {
            get
            {
                return _resourceLoader.GetString("PropertyAlbumArtist");
            }
        }
        #endregion

        #region PropertyBitRate
        /// <summary>
        ///   Looks up a localized string similar to: Bit rate
        /// </summary>
        public static string PropertyBitRate
        {
            get
            {
                return _resourceLoader.GetString("PropertyBitRate");
            }
        }
        #endregion

        #region PropertyContentType
        /// <summary>
        ///   Looks up a localized string similar to: Content type
        /// </summary>
        public static string PropertyContentType
        {
            get
            {
                return _resourceLoader.GetString("PropertyContentType");
            }
        }
        #endregion

        #region PropertyContributingArtists
        /// <summary>
        ///   Looks up a localized string similar to: Contributing artists
        /// </summary>
        public static string PropertyContributingArtists
        {
            get
            {
                return _resourceLoader.GetString("PropertyContributingArtists");
            }
        }
        #endregion

        #region PropertyFileLocation
        /// <summary>
        ///   Looks up a localized string similar to: File location
        /// </summary>
        public static string PropertyFileLocation
        {
            get
            {
                return _resourceLoader.GetString("PropertyFileLocation");
            }
        }
        #endregion

        #region PropertyFileType
        /// <summary>
        ///   Looks up a localized string similar to: File type
        /// </summary>
        public static string PropertyFileType
        {
            get
            {
                return _resourceLoader.GetString("PropertyFileType");
            }
        }
        #endregion

        #region PropertyGenre
        /// <summary>
        ///   Looks up a localized string similar to: Genre
        /// </summary>
        public static string PropertyGenre
        {
            get
            {
                return _resourceLoader.GetString("PropertyGenre");
            }
        }
        #endregion

        #region PropertyLastModified
        /// <summary>
        ///   Looks up a localized string similar to: Last modified
        /// </summary>
        public static string PropertyLastModified
        {
            get
            {
                return _resourceLoader.GetString("PropertyLastModified");
            }
        }
        #endregion

        #region PropertyLength
        /// <summary>
        ///   Looks up a localized string similar to: Length
        /// </summary>
        public static string PropertyLength
        {
            get
            {
                return _resourceLoader.GetString("PropertyLength");
            }
        }
        #endregion

        #region PropertyProducers
        /// <summary>
        ///   Looks up a localized string similar to: Producers
        /// </summary>
        public static string PropertyProducers
        {
            get
            {
                return _resourceLoader.GetString("PropertyProducers");
            }
        }
        #endregion

        #region PropertyResolution
        /// <summary>
        ///   Looks up a localized string similar to: Resolution
        /// </summary>
        public static string PropertyResolution
        {
            get
            {
                return _resourceLoader.GetString("PropertyResolution");
            }
        }
        #endregion

        #region PropertySize
        /// <summary>
        ///   Looks up a localized string similar to: Size
        /// </summary>
        public static string PropertySize
        {
            get
            {
                return _resourceLoader.GetString("PropertySize");
            }
        }
        #endregion

        #region PropertySubtitle
        /// <summary>
        ///   Looks up a localized string similar to: Subtitle
        /// </summary>
        public static string PropertySubtitle
        {
            get
            {
                return _resourceLoader.GetString("PropertySubtitle");
            }
        }
        #endregion

        #region PropertyTitle
        /// <summary>
        ///   Looks up a localized string similar to: Title
        /// </summary>
        public static string PropertyTitle
        {
            get
            {
                return _resourceLoader.GetString("PropertyTitle");
            }
        }
        #endregion

        #region PropertyTrack
        /// <summary>
        ///   Looks up a localized string similar to: Track
        /// </summary>
        public static string PropertyTrack
        {
            get
            {
                return _resourceLoader.GetString("PropertyTrack");
            }
        }
        #endregion

        #region PropertyWriters
        /// <summary>
        ///   Looks up a localized string similar to: Writers
        /// </summary>
        public static string PropertyWriters
        {
            get
            {
                return _resourceLoader.GetString("PropertyWriters");
            }
        }
        #endregion

        #region PropertyYear
        /// <summary>
        ///   Looks up a localized string similar to: Year
        /// </summary>
        public static string PropertyYear
        {
            get
            {
                return _resourceLoader.GetString("PropertyYear");
            }
        }
        #endregion

        #region Custom
        /// <summary>
        ///   Looks up a localized string similar to: Custom
        /// </summary>
        public static string Custom
        {
            get
            {
                return _resourceLoader.GetString("Custom");
            }
        }
        #endregion

        #region Network
        /// <summary>
        ///   Looks up a localized string similar to: Network
        /// </summary>
        public static string Network
        {
            get
            {
                return _resourceLoader.GetString("Network");
            }
        }
        #endregion

        #region NoNetworkDrivePanelHeader
        /// <summary>
        ///   Looks up a localized string similar to: No network device available
        /// </summary>
        public static string NoNetworkDrivePanelHeader
        {
            get
            {
                return _resourceLoader.GetString("NoNetworkDrivePanelHeader");
            }
        }
        #endregion

        #region NoNetworkDrivePanelSubtext
        /// <summary>
        ///   Looks up a localized string similar to: Can't find your media? Make sure your network device is paired in the Windows Settings.
        /// </summary>
        public static string NoNetworkDrivePanelSubtext
        {
            get
            {
                return _resourceLoader.GetString("NoNetworkDrivePanelSubtext");
            }
        }
        #endregion

        #region Recent
        /// <summary>
        ///   Looks up a localized string similar to: Recent
        /// </summary>
        public static string Recent
        {
            get
            {
                return _resourceLoader.GetString("Recent");
            }
        }
        #endregion

        #region VideoSettings
        /// <summary>
        ///   Looks up a localized string similar to: Video settings
        /// </summary>
        public static string VideoSettings
        {
            get
            {
                return _resourceLoader.GetString("VideoSettings");
            }
        }
        #endregion

        #region Open
        /// <summary>
        ///   Looks up a localized string similar to: Open
        /// </summary>
        public static string Open
        {
            get
            {
                return _resourceLoader.GetString("Open");
            }
        }
        #endregion

        #region ResumePositionNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Resume where you left off
        /// </summary>
        public static string ResumePositionNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("ResumePositionNotificationTitle");
            }
        }
        #endregion

        #region GoToPosition
        /// <summary>
        ///   Looks up a localized string similar to: Go to {0}
        /// </summary>
        public static string GoToPosition(string position)
        {
            return string.Format(_resourceLoader.GetString("GoToPosition"), position);
        }
        #endregion

        #region Albums
        /// <summary>
        ///   Looks up a localized string similar to: Albums
        /// </summary>
        public static string Albums
        {
            get
            {
                return _resourceLoader.GetString("Albums");
            }
        }
        #endregion

        #region Artists
        /// <summary>
        ///   Looks up a localized string similar to: Artists
        /// </summary>
        public static string Artists
        {
            get
            {
                return _resourceLoader.GetString("Artists");
            }
        }
        #endregion

        #region Songs
        /// <summary>
        ///   Looks up a localized string similar to: Songs
        /// </summary>
        public static string Songs
        {
            get
            {
                return _resourceLoader.GetString("Songs");
            }
        }
        #endregion

        #region OpenFilesToolTip
        /// <summary>
        ///   Looks up a localized string similar to: Browse for files to play
        /// </summary>
        public static string OpenFilesToolTip
        {
            get
            {
                return _resourceLoader.GetString("OpenFilesToolTip");
            }
        }
        #endregion

        #region AddFiles
        /// <summary>
        ///   Looks up a localized string similar to: Add file(s)
        /// </summary>
        public static string AddFiles
        {
            get
            {
                return _resourceLoader.GetString("AddFiles");
            }
        }
        #endregion

        #region AddFilesToPlayQueueToolTip
        /// <summary>
        ///   Looks up a localized string similar to: Browse for file(s) to add to the play queue
        /// </summary>
        public static string AddFilesToPlayQueueToolTip
        {
            get
            {
                return _resourceLoader.GetString("AddFilesToPlayQueueToolTip");
            }
        }
        #endregion

        #region AddMusicFolderToolTip
        /// <summary>
        ///   Looks up a localized string similar to: Add a folder to your music library
        /// </summary>
        public static string AddMusicFolderToolTip
        {
            get
            {
                return _resourceLoader.GetString("AddMusicFolderToolTip");
            }
        }
        #endregion

        #region AddUrl
        /// <summary>
        ///   Looks up a localized string similar to: Add media from URL
        /// </summary>
        public static string AddUrl
        {
            get
            {
                return _resourceLoader.GetString("AddUrl");
            }
        }
        #endregion

        #region AddVideoFolderToolTip
        /// <summary>
        ///   Looks up a localized string similar to: Add a folder to your video library
        /// </summary>
        public static string AddVideoFolderToolTip
        {
            get
            {
                return _resourceLoader.GetString("AddVideoFolderToolTip");
            }
        }
        #endregion

        #region OpenFolder
        /// <summary>
        ///   Looks up a localized string similar to: Open folder
        /// </summary>
        public static string OpenFolder
        {
            get
            {
                return _resourceLoader.GetString("OpenFolder");
            }
        }
        #endregion

        #region OpenUrl
        /// <summary>
        ///   Looks up a localized string similar to: Open URL
        /// </summary>
        public static string OpenUrl
        {
            get
            {
                return _resourceLoader.GetString("OpenUrl");
            }
        }
        #endregion

        #region RunTime
        /// <summary>
        ///   Looks up a localized string similar to: {0} run time
        /// </summary>
        public static string RunTime(string runtime)
        {
            return string.Format(_resourceLoader.GetString("RunTime"), runtime);
        }
        #endregion

        #region AllVideos
        /// <summary>
        ///   Looks up a localized string similar to: All videos
        /// </summary>
        public static string AllVideos
        {
            get
            {
                return _resourceLoader.GetString("AllVideos");
            }
        }
        #endregion

        #region VideoFolders
        /// <summary>
        ///   Looks up a localized string similar to: Video folders
        /// </summary>
        public static string VideoFolders
        {
            get
            {
                return _resourceLoader.GetString("VideoFolders");
            }
        }
        #endregion

        #region SearchResultHeader
        /// <summary>
        ///   Looks up a localized string similar to: Search results for "{0}"
        /// </summary>
        public static string SearchResultHeader(string query)
        {
            return string.Format(_resourceLoader.GetString("SearchResultHeader"), query);
        }
        #endregion

        #region SeeAll
        /// <summary>
        ///   Looks up a localized string similar to: See all
        /// </summary>
        public static string SeeAll
        {
            get
            {
                return _resourceLoader.GetString("SeeAll");
            }
        }
        #endregion

        #region SearchResultArtistHeader
        /// <summary>
        ///   Looks up a localized string similar to: Artist results for "{0}"
        /// </summary>
        public static string SearchResultArtistHeader(string query)
        {
            return string.Format(_resourceLoader.GetString("SearchResultArtistHeader"), query);
        }
        #endregion

        #region SearchResultAlbumHeader
        /// <summary>
        ///   Looks up a localized string similar to: Album results for "{0}"
        /// </summary>
        public static string SearchResultAlbumHeader(string query)
        {
            return string.Format(_resourceLoader.GetString("SearchResultAlbumHeader"), query);
        }
        #endregion

        #region SearchResultSongHeader
        /// <summary>
        ///   Looks up a localized string similar to: Song results for "{0}"
        /// </summary>
        public static string SearchResultSongHeader(string query)
        {
            return string.Format(_resourceLoader.GetString("SearchResultSongHeader"), query);
        }
        #endregion

        #region SearchResultVideoHeader
        /// <summary>
        ///   Looks up a localized string similar to: Video results for "{0}"
        /// </summary>
        public static string SearchResultVideoHeader(string query)
        {
            return string.Format(_resourceLoader.GetString("SearchResultVideoHeader"), query);
        }
        #endregion

        #region AccessDeniedMessage
        /// <summary>
        ///   Looks up a localized string similar to: Access denied. Please verify your privacy settings to ensure Screenbox has sufficient permissions.
        /// </summary>
        public static string AccessDeniedMessage
        {
            get
            {
                return _resourceLoader.GetString("AccessDeniedMessage");
            }
        }
        #endregion

        #region AccessDeniedMusicLibraryTitle
        /// <summary>
        ///   Looks up a localized string similar to: Can't access music library
        /// </summary>
        public static string AccessDeniedMusicLibraryTitle
        {
            get
            {
                return _resourceLoader.GetString("AccessDeniedMusicLibraryTitle");
            }
        }
        #endregion

        #region AccessDeniedPicturesLibraryTitle
        /// <summary>
        ///   Looks up a localized string similar to: Can't access pictures library
        /// </summary>
        public static string AccessDeniedPicturesLibraryTitle
        {
            get
            {
                return _resourceLoader.GetString("AccessDeniedPicturesLibraryTitle");
            }
        }
        #endregion

        #region AccessDeniedVideosLibraryTitle
        /// <summary>
        ///   Looks up a localized string similar to: Can't access videos library
        /// </summary>
        public static string AccessDeniedVideosLibraryTitle
        {
            get
            {
                return _resourceLoader.GetString("AccessDeniedVideosLibraryTitle");
            }
        }
        #endregion

        #region NoVideosPanelHeader
        /// <summary>
        ///   Looks up a localized string similar to: We couldn't find any videos
        /// </summary>
        public static string NoVideosPanelHeader
        {
            get
            {
                return _resourceLoader.GetString("NoVideosPanelHeader");
            }
        }
        #endregion

        #region NoVideosPanelSubtext
        /// <summary>
        ///   Looks up a localized string similar to: Your video library doesn't contain any video content.
        /// </summary>
        public static string NoVideosPanelSubtext
        {
            get
            {
                return _resourceLoader.GetString("NoVideosPanelSubtext");
            }
        }
        #endregion

        #region OpenPrivacySettingsButtonText
        /// <summary>
        ///   Looks up a localized string similar to: Open privacy settings
        /// </summary>
        public static string OpenPrivacySettingsButtonText
        {
            get
            {
                return _resourceLoader.GetString("OpenPrivacySettingsButtonText");
            }
        }
        #endregion

        #region FailedToLoadMediaNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Unable to load media
        /// </summary>
        public static string FailedToLoadMediaNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("FailedToLoadMediaNotificationTitle");
            }
        }
        #endregion

        #region AspectRatio
        /// <summary>
        ///   Looks up a localized string similar to: Aspect ratio
        /// </summary>
        public static string AspectRatio
        {
            get
            {
                return _resourceLoader.GetString("AspectRatio");
            }
        }
        #endregion

        #region Fit
        /// <summary>
        ///   Looks up a localized string similar to: Fit
        /// </summary>
        public static string Fit
        {
            get
            {
                return _resourceLoader.GetString("Fit");
            }
        }
        #endregion

        #region Fill
        /// <summary>
        ///   Looks up a localized string similar to: Fill
        /// </summary>
        public static string Fill
        {
            get
            {
                return _resourceLoader.GetString("Fill");
            }
        }
        #endregion

        #region CustomAspectRatio
        /// <summary>
        ///   Looks up a localized string similar to: Custom aspect ratio
        /// </summary>
        public static string CustomAspectRatio
        {
            get
            {
                return _resourceLoader.GetString("CustomAspectRatio");
            }
        }
        #endregion

        #region CustomPlaybackSpeed
        /// <summary>
        ///   Looks up a localized string similar to: Custom playback speed
        /// </summary>
        public static string CustomPlaybackSpeed
        {
            get
            {
                return _resourceLoader.GetString("CustomPlaybackSpeed");
            }
        }
        #endregion

        #region None
        /// <summary>
        ///   Looks up a localized string similar to: None
        /// </summary>
        public static string None
        {
            get
            {
                return _resourceLoader.GetString("None");
            }
        }
        #endregion

        #region ScaleStatus
        /// <summary>
        ///   Looks up a localized string similar to: Scale: {0}
        /// </summary>
        public static string ScaleStatus(string scale)
        {
            return string.Format(_resourceLoader.GetString("ScaleStatus"), scale);
        }
        #endregion

        #region SubtitleStatus
        /// <summary>
        ///   Looks up a localized string similar to: Subtitle: {0}
        /// </summary>
        public static string SubtitleStatus(string name)
        {
            return string.Format(_resourceLoader.GetString("SubtitleStatus"), name);
        }
        #endregion

        #region TrackIndex
        /// <summary>
        ///   Looks up a localized string similar to: Track {0}
        /// </summary>
        public static string TrackIndex(int index)
        {
            return string.Format(_resourceLoader.GetString("TrackIndex"), index);
        }
        #endregion

        #region AddSubtitle
        /// <summary>
        ///   Looks up a localized string similar to: Add subtitle
        /// </summary>
        public static string AddSubtitle
        {
            get
            {
                return _resourceLoader.GetString("AddSubtitle");
            }
        }
        #endregion

        #region Audio
        /// <summary>
        ///   Looks up a localized string similar to: Audio
        /// </summary>
        public static string Audio
        {
            get
            {
                return _resourceLoader.GetString("Audio");
            }
        }
        #endregion

        #region SettingsCategoryAbout
        /// <summary>
        ///   Looks up a localized string similar to: About
        /// </summary>
        public static string SettingsCategoryAbout
        {
            get
            {
                return _resourceLoader.GetString("SettingsCategoryAbout");
            }
        }
        #endregion

        #region SettingsCategoryGeneral
        /// <summary>
        ///   Looks up a localized string similar to: General
        /// </summary>
        public static string SettingsCategoryGeneral
        {
            get
            {
                return _resourceLoader.GetString("SettingsCategoryGeneral");
            }
        }
        #endregion

        #region SettingsCategoryLibraries
        /// <summary>
        ///   Looks up a localized string similar to: Libraries
        /// </summary>
        public static string SettingsCategoryLibraries
        {
            get
            {
                return _resourceLoader.GetString("SettingsCategoryLibraries");
            }
        }
        #endregion

        #region SettingsCategoryPlayer
        /// <summary>
        ///   Looks up a localized string similar to: Player
        /// </summary>
        public static string SettingsCategoryPlayer
        {
            get
            {
                return _resourceLoader.GetString("SettingsCategoryPlayer");
            }
        }
        #endregion

        #region SettingsMusicLibraryLocationsHeader
        /// <summary>
        ///   Looks up a localized string similar to: Music library locations
        /// </summary>
        public static string SettingsMusicLibraryLocationsHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsMusicLibraryLocationsHeader");
            }
        }
        #endregion

        #region SettingsShowRecentHeader
        /// <summary>
        ///   Looks up a localized string similar to: Show recent
        /// </summary>
        public static string SettingsShowRecentHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsShowRecentHeader");
            }
        }
        #endregion

        #region SettingsShowControlsHeader
        /// <summary>
        ///   Looks up a localized string similar to: Display controls on pause
        /// </summary>
        public static string SettingsShowControlsHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsShowControlsHeader");
            }
        }
        #endregion

        #region SettingsVideoLibraryLocationsHeader
        /// <summary>
        ///   Looks up a localized string similar to: Video library locations
        /// </summary>
        public static string SettingsVideoLibraryLocationsHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsVideoLibraryLocationsHeader");
            }
        }
        #endregion

        #region Subtitles
        /// <summary>
        ///   Looks up a localized string similar to: Subtitles
        /// </summary>
        public static string Subtitles
        {
            get
            {
                return _resourceLoader.GetString("Subtitles");
            }
        }
        #endregion

        #region SettingsShowRecentDescription
        /// <summary>
        ///   Looks up a localized string similar to: Display recently played media on the home page
        /// </summary>
        public static string SettingsShowRecentDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsShowRecentDescription");
            }
        }
        #endregion

        #region SettingsClearRecentHeader
        /// <summary>
        ///   Looks up a localized string similar to: Clear recent media history
        /// </summary>
        public static string SettingsClearRecentHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsClearRecentHeader");
            }
        }
        #endregion

        #region SettingsAutoResizeHeader
        /// <summary>
        ///   Looks up a localized string similar to: Auto resize
        /// </summary>
        public static string SettingsAutoResizeHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsAutoResizeHeader");
            }
        }
        #endregion

        #region SettingsAutoResizeDescription
        /// <summary>
        ///   Looks up a localized string similar to: Resize the app window to best match a video's native resolution on playback
        /// </summary>
        public static string SettingsAutoResizeDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsAutoResizeDescription");
            }
        }
        #endregion

        #region SettingsVolumeBoostHeader
        /// <summary>
        ///   Looks up a localized string similar to: Volume boost
        /// </summary>
        public static string SettingsVolumeBoostHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsVolumeBoostHeader");
            }
        }
        #endregion

        #region SettingsVolumeBoostDescription
        /// <summary>
        ///   Looks up a localized string similar to: Allow the maximum volume to go above 100%
        /// </summary>
        public static string SettingsVolumeBoostDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsVolumeBoostDescription");
            }
        }
        #endregion

        #region SettingsGesturesHeader
        /// <summary>
        ///   Looks up a localized string similar to: Gestures
        /// </summary>
        public static string SettingsGesturesHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsGesturesHeader");
            }
        }
        #endregion

        #region SettingsGestureSeek
        /// <summary>
        ///   Looks up a localized string similar to: Drag horizontally to seek
        /// </summary>
        public static string SettingsGestureSeek
        {
            get
            {
                return _resourceLoader.GetString("SettingsGestureSeek");
            }
        }
        #endregion

        #region SettingsGestureVolume
        /// <summary>
        ///   Looks up a localized string similar to: Drag vertically to adjust volume
        /// </summary>
        public static string SettingsGestureVolume
        {
            get
            {
                return _resourceLoader.GetString("SettingsGestureVolume");
            }
        }
        #endregion

        #region SettingsGestureTap
        /// <summary>
        ///   Looks up a localized string similar to: Tap anywhere to play or pause
        /// </summary>
        public static string SettingsGestureTap
        {
            get
            {
                return _resourceLoader.GetString("SettingsGestureTap");
            }
        }
        #endregion

        #region AppFriendlyName
        /// <summary>
        ///   Looks up a localized string similar to: Screenbox Media Player
        /// </summary>
        public static string AppFriendlyName
        {
            get
            {
                return _resourceLoader.GetString("AppFriendlyName");
            }
        }
        #endregion

        #region HyperlinkSourceCode
        /// <summary>
        ///   Looks up a localized string similar to: Source code
        /// </summary>
        public static string HyperlinkSourceCode
        {
            get
            {
                return _resourceLoader.GetString("HyperlinkSourceCode");
            }
        }
        #endregion

        #region HyperlinkDiscord
        /// <summary>
        ///   Looks up a localized string similar to: Discord
        /// </summary>
        public static string HyperlinkDiscord
        {
            get
            {
                return _resourceLoader.GetString("HyperlinkDiscord");
            }
        }
        #endregion

        #region HyperlinkSponsor
        /// <summary>
        ///   Looks up a localized string similar to: Support the development
        /// </summary>
        public static string HyperlinkSponsor
        {
            get
            {
                return _resourceLoader.GetString("HyperlinkSponsor");
            }
        }
        #endregion

        #region Always
        /// <summary>
        ///   Looks up a localized string similar to: Always
        /// </summary>
        public static string Always
        {
            get
            {
                return _resourceLoader.GetString("Always");
            }
        }
        #endregion

        #region Never
        /// <summary>
        ///   Looks up a localized string similar to: Never
        /// </summary>
        public static string Never
        {
            get
            {
                return _resourceLoader.GetString("Never");
            }
        }
        #endregion

        #region OnLaunch
        /// <summary>
        ///   Looks up a localized string similar to: On launch
        /// </summary>
        public static string OnLaunch
        {
            get
            {
                return _resourceLoader.GetString("OnLaunch");
            }
        }
        #endregion

        #region VersionText
        /// <summary>
        ///   Looks up a localized string similar to: Version {0}
        /// </summary>
        public static string VersionText
        {
            get
            {
                return string.Format(_resourceLoader.GetString("VersionText"), ReswPlusLib.Macros.AppVersionFull);
            }
        }
        #endregion

        #region SubtitleAddedNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Subtitle added
        /// </summary>
        public static string SubtitleAddedNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("SubtitleAddedNotificationTitle");
            }
        }
        #endregion

        #region CriticalError
        /// <summary>
        ///   Looks up a localized string similar to: Critical error
        /// </summary>
        public static string CriticalError
        {
            get
            {
                return _resourceLoader.GetString("CriticalError");
            }
        }
        #endregion

        #region CriticalErrorDirect3D11NotAvailable
        /// <summary>
        ///   Looks up a localized string similar to: No compatible renderer available. Please make sure Direct3D 11 is available on your device.
        /// </summary>
        public static string CriticalErrorDirect3D11NotAvailable
        {
            get
            {
                return _resourceLoader.GetString("CriticalErrorDirect3D11NotAvailable");
            }
        }
        #endregion

        #region FailedToOpenFilesNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Failed to open file(s)
        /// </summary>
        public static string FailedToOpenFilesNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("FailedToOpenFilesNotificationTitle");
            }
        }
        #endregion

        #region OpenUrlPlaceholder
        /// <summary>
        ///   Looks up a localized string similar to: Enter the URL for a file or stream
        /// </summary>
        public static string OpenUrlPlaceholder
        {
            get
            {
                return _resourceLoader.GetString("OpenUrlPlaceholder");
            }
        }
        #endregion

        #region OpenConnectedDevicesSettingsButtonText
        /// <summary>
        ///   Looks up a localized string similar to: Open Connected Devices settings
        /// </summary>
        public static string OpenConnectedDevicesSettingsButtonText
        {
            get
            {
                return _resourceLoader.GetString("OpenConnectedDevicesSettingsButtonText");
            }
        }
        #endregion

        #region SetPlaybackOptions
        /// <summary>
        ///   Looks up a localized string similar to: Set playback options
        /// </summary>
        public static string SetPlaybackOptions
        {
            get
            {
                return _resourceLoader.GetString("SetPlaybackOptions");
            }
        }
        #endregion

        #region Set
        /// <summary>
        ///   Looks up a localized string similar to: Set
        /// </summary>
        public static string Set
        {
            get
            {
                return _resourceLoader.GetString("Set");
            }
        }
        #endregion

        #region SetAndPlay
        /// <summary>
        ///   Looks up a localized string similar to: Set and Play
        /// </summary>
        public static string SetAndPlay
        {
            get
            {
                return _resourceLoader.GetString("SetAndPlay");
            }
        }
        #endregion

        #region SetPlaybackOptionsHelpTextLine1
        /// <summary>
        ///   Looks up a localized string similar to: Set VLC options that apply to a stream.
        /// </summary>
        public static string SetPlaybackOptionsHelpTextLine1
        {
            get
            {
                return _resourceLoader.GetString("SetPlaybackOptionsHelpTextLine1");
            }
        }
        #endregion

        #region SetPlaybackOptionsHelpTextLine2
        /// <summary>
        ///   Looks up a localized string similar to: Some options may only be set globally.
        /// </summary>
        public static string SetPlaybackOptionsHelpTextLine2
        {
            get
            {
                return _resourceLoader.GetString("SetPlaybackOptionsHelpTextLine2");
            }
        }
        #endregion

        #region VlcCommandLineHelpLink
        /// <summary>
        ///   Looks up a localized string similar to: VLC command-line help
        /// </summary>
        public static string VlcCommandLineHelpLink
        {
            get
            {
                return _resourceLoader.GetString("VlcCommandLineHelpLink");
            }
        }
        #endregion

        #region VlcCommandLineHelpText
        /// <summary>
        ///   Looks up a localized string similar to: See {0} for the full list of available options.
        /// </summary>
        public static string VlcCommandLineHelpText
        {
            get
            {
                return _resourceLoader.GetString("VlcCommandLineHelpText");
            }
        }
        #endregion

        #region SettingsCategoryAdvanced
        /// <summary>
        ///   Looks up a localized string similar to: Advanced
        /// </summary>
        public static string SettingsCategoryAdvanced
        {
            get
            {
                return _resourceLoader.GetString("SettingsCategoryAdvanced");
            }
        }
        #endregion

        #region SettingsAdvancedModeHeader
        /// <summary>
        ///   Looks up a localized string similar to: Advanced mode
        /// </summary>
        public static string SettingsAdvancedModeHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsAdvancedModeHeader");
            }
        }
        #endregion

        #region SettingsAdvancedModeDescription
        /// <summary>
        ///   Looks up a localized string similar to: Advanced mode allows you to customize LibVLC's behavior using command line arguments
        /// </summary>
        public static string SettingsAdvancedModeDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsAdvancedModeDescription");
            }
        }
        #endregion

        #region SettingsGlobalArgumentsHeader
        /// <summary>
        ///   Looks up a localized string similar to: Global arguments
        /// </summary>
        public static string SettingsGlobalArgumentsHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsGlobalArgumentsHeader");
            }
        }
        #endregion

        #region SettingsGlobalArgumentsDescription
        /// <summary>
        ///   Looks up a localized string similar to: Command line arguments that apply to all media playback
        /// </summary>
        public static string SettingsGlobalArgumentsDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsGlobalArgumentsDescription");
            }
        }
        #endregion

        #region RemoveFolder
        /// <summary>
        ///   Looks up a localized string similar to: Remove folder
        /// </summary>
        public static string RemoveFolder
        {
            get
            {
                return _resourceLoader.GetString("RemoveFolder");
            }
        }
        #endregion

        #region PendingChanges
        /// <summary>
        ///   Looks up a localized string similar to: Pending changes
        /// </summary>
        public static string PendingChanges
        {
            get
            {
                return _resourceLoader.GetString("PendingChanges");
            }
        }
        #endregion

        #region RelaunchForChangesMessage
        /// <summary>
        ///   Looks up a localized string similar to: Relaunch the app for changes to take effect
        /// </summary>
        public static string RelaunchForChangesMessage
        {
            get
            {
                return _resourceLoader.GetString("RelaunchForChangesMessage");
            }
        }
        #endregion

        #region FailedToInitializeNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Failed to initialize
        /// </summary>
        public static string FailedToInitializeNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("FailedToInitializeNotificationTitle");
            }
        }
        #endregion

        #region RelatedLinks
        /// <summary>
        ///   Looks up a localized string similar to: Related links
        /// </summary>
        public static string RelatedLinks
        {
            get
            {
                return _resourceLoader.GetString("RelatedLinks");
            }
        }
        #endregion

        #region PrivacyPolicy
        /// <summary>
        ///   Looks up a localized string similar to: Privacy policy
        /// </summary>
        public static string PrivacyPolicy
        {
            get
            {
                return _resourceLoader.GetString("PrivacyPolicy");
            }
        }
        #endregion

        #region License
        /// <summary>
        ///   Looks up a localized string similar to: License
        /// </summary>
        public static string License
        {
            get
            {
                return _resourceLoader.GetString("License");
            }
        }
        #endregion

        #region HyperlinkTranslate
        /// <summary>
        ///   Looks up a localized string similar to: Help translate
        /// </summary>
        public static string HyperlinkTranslate
        {
            get
            {
                return _resourceLoader.GetString("HyperlinkTranslate");
            }
        }
        #endregion

        #region Options
        /// <summary>
        ///   Looks up a localized string similar to: Options
        /// </summary>
        public static string Options
        {
            get
            {
                return _resourceLoader.GetString("Options");
            }
        }
        #endregion

        #region TimingOffset
        /// <summary>
        ///   Looks up a localized string similar to: Timing offset
        /// </summary>
        public static string TimingOffset
        {
            get
            {
                return _resourceLoader.GetString("TimingOffset");
            }
        }
        #endregion

        #region PropertyComposers
        /// <summary>
        ///   Looks up a localized string similar to: Composers
        /// </summary>
        public static string PropertyComposers
        {
            get
            {
                return _resourceLoader.GetString("PropertyComposers");
            }
        }
        #endregion

        #region FailedToAddFolderNotificationTitle
        /// <summary>
        ///   Looks up a localized string similar to: Couldn't add folder
        /// </summary>
        public static string FailedToAddFolderNotificationTitle
        {
            get
            {
                return _resourceLoader.GetString("FailedToAddFolderNotificationTitle");
            }
        }
        #endregion

        #region OpenAlbum
        /// <summary>
        ///   Looks up a localized string similar to: Open album
        /// </summary>
        public static string OpenAlbum
        {
            get
            {
                return _resourceLoader.GetString("OpenAlbum");
            }
        }
        #endregion

        #region OpenArtist
        /// <summary>
        ///   Looks up a localized string similar to: Open artist
        /// </summary>
        public static string OpenArtist
        {
            get
            {
                return _resourceLoader.GetString("OpenArtist");
            }
        }
        #endregion

        #region RefreshLibraries
        /// <summary>
        ///   Looks up a localized string similar to: Refresh libraries
        /// </summary>
        public static string RefreshLibraries
        {
            get
            {
                return _resourceLoader.GetString("RefreshLibraries");
            }
        }
        #endregion

        #region Refresh
        /// <summary>
        ///   Looks up a localized string similar to: Refresh
        /// </summary>
        public static string Refresh
        {
            get
            {
                return _resourceLoader.GetString("Refresh");
            }
        }
        #endregion

        #region SettingsUseIndexerHeader
        /// <summary>
        ///   Looks up a localized string similar to: Use indexer for scanning
        /// </summary>
        public static string SettingsUseIndexerHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsUseIndexerHeader");
            }
        }
        #endregion

        #region SettingsUseIndexerDescription
        /// <summary>
        ///   Looks up a localized string similar to: Speed up the library scanning by using the system index when available. Turn this off if you are not seeing all the media from your library locations.
        /// </summary>
        public static string SettingsUseIndexerDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsUseIndexerDescription");
            }
        }
        #endregion

        #region ManageSystemIndexingLink
        /// <summary>
        ///   Looks up a localized string similar to: Manage system indexing settings
        /// </summary>
        public static string ManageSystemIndexingLink
        {
            get
            {
                return _resourceLoader.GetString("ManageSystemIndexingLink");
            }
        }
        #endregion

        #region SetArguments
        /// <summary>
        ///   Looks up a localized string similar to: Set arguments
        /// </summary>
        public static string SetArguments
        {
            get
            {
                return _resourceLoader.GetString("SetArguments");
            }
        }
        #endregion

        #region SettingsSearchRemovableStorageHeader
        /// <summary>
        ///   Looks up a localized string similar to: Search removable storage
        /// </summary>
        public static string SettingsSearchRemovableStorageHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsSearchRemovableStorageHeader");
            }
        }
        #endregion

        #region SettingsSearchRemovableStorageDescription
        /// <summary>
        ///   Looks up a localized string similar to: Include media from storage devices like USB sticks in your libraries
        /// </summary>
        public static string SettingsSearchRemovableStorageDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsSearchRemovableStorageDescription");
            }
        }
        #endregion

        #region ActiveArguments
        /// <summary>
        ///   Looks up a localized string similar to: Active arguments
        /// </summary>
        public static string ActiveArguments
        {
            get
            {
                return _resourceLoader.GetString("ActiveArguments");
            }
        }
        #endregion

        #region SettingsUseMultipleInstancesHeader
        /// <summary>
        ///   Looks up a localized string similar to: Allow multiple instances
        /// </summary>
        public static string SettingsUseMultipleInstancesHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsUseMultipleInstancesHeader");
            }
        }
        #endregion

        #region SettingsUseMultipleInstancesDescription
        /// <summary>
        ///   Looks up a localized string similar to: Always open media files in a new instance
        /// </summary>
        public static string SettingsUseMultipleInstancesDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsUseMultipleInstancesDescription");
            }
        }
        #endregion

        #region SortBy
        /// <summary>
        ///   Looks up a localized string similar to: Sort by
        /// </summary>
        public static string SortBy
        {
            get
            {
                return _resourceLoader.GetString("SortBy");
            }
        }
        #endregion

        #region Artist
        /// <summary>
        ///   Looks up a localized string similar to: Artist
        /// </summary>
        public static string Artist
        {
            get
            {
                return _resourceLoader.GetString("Artist");
            }
        }
        #endregion

        #region ReleasedYear
        /// <summary>
        ///   Looks up a localized string similar to: Released year
        /// </summary>
        public static string ReleasedYear
        {
            get
            {
                return _resourceLoader.GetString("ReleasedYear");
            }
        }
        #endregion

        #region SettingsAudioVisualHeader
        /// <summary>
        ///   Looks up a localized string similar to: Audio visual
        /// </summary>
        public static string SettingsAudioVisualHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsAudioVisualHeader");
            }
        }
        #endregion

        #region GetLivelyApp
        /// <summary>
        ///   Looks up a localized string similar to: Get Lively Wallpaper
        /// </summary>
        public static string GetLivelyApp
        {
            get
            {
                return _resourceLoader.GetString("GetLivelyApp");
            }
        }
        #endregion

        #region GetLivelyVisuals
        /// <summary>
        ///   Looks up a localized string similar to: Download visuals on GitHub
        /// </summary>
        public static string GetLivelyVisuals
        {
            get
            {
                return _resourceLoader.GetString("GetLivelyVisuals");
            }
        }
        #endregion

        #region BrowseFiles
        /// <summary>
        ///   Looks up a localized string similar to: Browse file(s)
        /// </summary>
        public static string BrowseFiles
        {
            get
            {
                return _resourceLoader.GetString("BrowseFiles");
            }
        }
        #endregion

        #region Default
        /// <summary>
        ///   Looks up a localized string similar to: Default
        /// </summary>
        public static string Default
        {
            get
            {
                return _resourceLoader.GetString("Default");
            }
        }
        #endregion

        #region SettingsAudioVisualDescription
        /// <summary>
        ///   Looks up a localized string similar to: Choose an imagery style to be displayed on audio playback
        /// </summary>
        public static string SettingsAudioVisualDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsAudioVisualDescription");
            }
        }
        #endregion

        #region PoweredByLivelyWallpaper
        /// <summary>
        ///   Looks up a localized string similar to: Powered by Lively Wallpaper
        /// </summary>
        public static string PoweredByLivelyWallpaper
        {
            get
            {
                return _resourceLoader.GetString("PoweredByLivelyWallpaper");
            }
        }
        #endregion

        #region SettingsImportVisualsHeader
        /// <summary>
        ///   Looks up a localized string similar to: Import visuals
        /// </summary>
        public static string SettingsImportVisualsHeader
        {
            get
            {
                return _resourceLoader.GetString("SettingsImportVisualsHeader");
            }
        }
        #endregion

        #region SettingsImportVisualsDescription
        /// <summary>
        ///   Looks up a localized string similar to: Get more visuals via the Lively Wallpaper app or download them from GitHub
        /// </summary>
        public static string SettingsImportVisualsDescription
        {
            get
            {
                return _resourceLoader.GetString("SettingsImportVisualsDescription");
            }
        }
        #endregion

        #region DateAdded
        /// <summary>
        ///   Looks up a localized string similar to: Date added
        /// </summary>
        public static string DateAdded
        {
            get
            {
                return _resourceLoader.GetString("DateAdded");
            }
        }
        #endregion
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("DotNetPlus.ReswPlus", "2.1.3")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class ResourcesExtension: MarkupExtension
    {
        public enum KeyEnum
        {
            __Undefined = 0,
            CompactOverlayToggle,
            MuteToggle,
            FullscreenToggle,
            RepeatMode,
            ItemsSelected,
            ItemsCount,
            LocationSpecified,
            ShuffleMode,
            SongsCount,
            AlbumsCount,
            FrameSavedNotificationTitle,
            SaveCurrentFrame,
            Loop,
            PlaybackSpeed,
            VolumeChangeStatusMessage,
            FailedToSaveFrameNotificationTitle,
            ChapterName,
            FailedToLoadSubtitleNotificationTitle,
            Back,
            AudioAndCaption,
            Volume,
            Seek,
            Next,
            Previous,
            Play,
            Pause,
            More,
            PlayQueue,
            AddToQueue,
            ClearSelection,
            Remove,
            PlayNext,
            MoveUp,
            MoveDown,
            IsPlaying,
            Videos,
            EmptyFolder,
            RestoreView,
            Cast,
            StopCast,
            CastingTo,
            CastToDevice,
            Disable,
            Music,
            ShuffleAndPlay,
            UnknownArtist,
            UnknownAlbum,
            UnknownGenre,
            AddFolder,
            Clear,
            Home,
            Multiselect,
            NoMusicPanelHeader,
            NoMusicPanelSubtext,
            OpenFiles,
            SearchBoxPlaceholderText,
            Settings,
            WelcomePanelHeader,
            WelcomePanelSubtext,
            Properties,
            Close,
            OpenFileLocation,
            PropertyAlbum,
            PropertyAlbumArtist,
            PropertyBitRate,
            PropertyContentType,
            PropertyContributingArtists,
            PropertyFileLocation,
            PropertyFileType,
            PropertyGenre,
            PropertyLastModified,
            PropertyLength,
            PropertyProducers,
            PropertyResolution,
            PropertySize,
            PropertySubtitle,
            PropertyTitle,
            PropertyTrack,
            PropertyWriters,
            PropertyYear,
            Custom,
            Network,
            NoNetworkDrivePanelHeader,
            NoNetworkDrivePanelSubtext,
            Recent,
            VideoSettings,
            Open,
            ResumePositionNotificationTitle,
            GoToPosition,
            Albums,
            Artists,
            Songs,
            OpenFilesToolTip,
            AddFiles,
            AddFilesToPlayQueueToolTip,
            AddMusicFolderToolTip,
            AddUrl,
            AddVideoFolderToolTip,
            OpenFolder,
            OpenUrl,
            RunTime,
            AllVideos,
            VideoFolders,
            SearchResultHeader,
            SeeAll,
            SearchResultArtistHeader,
            SearchResultAlbumHeader,
            SearchResultSongHeader,
            SearchResultVideoHeader,
            AccessDeniedMessage,
            AccessDeniedMusicLibraryTitle,
            AccessDeniedPicturesLibraryTitle,
            AccessDeniedVideosLibraryTitle,
            NoVideosPanelHeader,
            NoVideosPanelSubtext,
            OpenPrivacySettingsButtonText,
            FailedToLoadMediaNotificationTitle,
            AspectRatio,
            Fit,
            Fill,
            CustomAspectRatio,
            CustomPlaybackSpeed,
            None,
            ScaleStatus,
            SubtitleStatus,
            TrackIndex,
            AddSubtitle,
            Audio,
            SettingsCategoryAbout,
            SettingsCategoryGeneral,
            SettingsCategoryLibraries,
            SettingsCategoryPlayer,
            SettingsMusicLibraryLocationsHeader,
            SettingsShowRecentHeader,
            SettingsShowControlsHeader,
            SettingsVideoLibraryLocationsHeader,
            Subtitles,
            SettingsShowRecentDescription,
            SettingsClearRecentHeader,
            SettingsAutoResizeHeader,
            SettingsAutoResizeDescription,
            SettingsVolumeBoostHeader,
            SettingsVolumeBoostDescription,
            SettingsGesturesHeader,
            SettingsGestureSeek,
            SettingsGestureVolume,
            SettingsGestureTap,
            AppFriendlyName,
            HyperlinkSourceCode,
            HyperlinkDiscord,
            HyperlinkSponsor,
            Always,
            Never,
            OnLaunch,
            VersionText,
            SubtitleAddedNotificationTitle,
            CriticalError,
            CriticalErrorDirect3D11NotAvailable,
            FailedToOpenFilesNotificationTitle,
            OpenUrlPlaceholder,
            OpenConnectedDevicesSettingsButtonText,
            SetPlaybackOptions,
            Set,
            SetAndPlay,
            SetPlaybackOptionsHelpTextLine1,
            SetPlaybackOptionsHelpTextLine2,
            VlcCommandLineHelpLink,
            VlcCommandLineHelpText,
            SettingsCategoryAdvanced,
            SettingsAdvancedModeHeader,
            SettingsAdvancedModeDescription,
            SettingsGlobalArgumentsHeader,
            SettingsGlobalArgumentsDescription,
            RemoveFolder,
            PendingChanges,
            RelaunchForChangesMessage,
            FailedToInitializeNotificationTitle,
            RelatedLinks,
            PrivacyPolicy,
            License,
            HyperlinkTranslate,
            Options,
            TimingOffset,
            PropertyComposers,
            FailedToAddFolderNotificationTitle,
            OpenAlbum,
            OpenArtist,
            RefreshLibraries,
            Refresh,
            SettingsUseIndexerHeader,
            SettingsUseIndexerDescription,
            ManageSystemIndexingLink,
            SetArguments,
            SettingsSearchRemovableStorageHeader,
            SettingsSearchRemovableStorageDescription,
            ActiveArguments,
            SettingsUseMultipleInstancesHeader,
            SettingsUseMultipleInstancesDescription,
            SortBy,
            Artist,
            ReleasedYear,
            SettingsAudioVisualHeader,
            GetLivelyApp,
            GetLivelyVisuals,
            BrowseFiles,
            Default,
            SettingsAudioVisualDescription,
            PoweredByLivelyWallpaper,
            SettingsImportVisualsHeader,
            SettingsImportVisualsDescription,
            DateAdded,
        }

        private static ResourceLoader _resourceLoader;
        static ResourcesExtension()
        {
            _resourceLoader = ResourceLoader.GetForViewIndependentUse("Resources");
        }
        public KeyEnum Key { get; set;}
        public IValueConverter Converter { get; set;}
        public object ConverterParameter { get; set;}
        protected override object ProvideValue()
        {
            string res;
            if(Key == KeyEnum.__Undefined)
            {
                res = "";
            }
            else
            {
                res = _resourceLoader.GetString(Key.ToString());
            }
            return Converter == null ? res : Converter.Convert(res, typeof(String), ConverterParameter, null);
        }
    }
} //Screenbox.Strings

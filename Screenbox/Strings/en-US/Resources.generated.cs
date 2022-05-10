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

        #region PlayPauseToggle
        /// <summary>
        ///   Get the variant version of the string similar to: Play
        /// </summary>
        public static string PlayPauseToggle(object variantId)
        {
            try
            {
                return PlayPauseToggle(Convert.ToInt64(variantId));
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///   Get the variant version of the string similar to: Play
        /// </summary>
        public static string PlayPauseToggle(long variantId)
        {
            return _resourceLoader.GetString("PlayPauseToggle_Variant" + variantId);
        }
        #endregion

        #region ZoomToFit
        /// <summary>
        ///   Looks up a localized string similar to: Zoom to fit
        /// </summary>
        public static string ZoomToFit
        {
            get
            {
                return _resourceLoader.GetString("ZoomToFit");
            }
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

        #region PictureInPicture
        /// <summary>
        ///   Looks up a localized string similar to: Picture in picture
        /// </summary>
        public static string PictureInPicture
        {
            get
            {
                return _resourceLoader.GetString("PictureInPicture");
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

        #region ExitCompactOverlay
        /// <summary>
        ///   Looks up a localized string similar to: Exit picture in picture
        /// </summary>
        public static string ExitCompactOverlay
        {
            get
            {
                return _resourceLoader.GetString("ExitCompactOverlay");
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
            MuteToggle,
            FullscreenToggle,
            PlayPauseToggle,
            ZoomToFit,
            FrameSavedNotificationTitle,
            PictureInPicture,
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
            ExitCompactOverlay,
            Next,
            Previous,
            More,
            PlayQueue,
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

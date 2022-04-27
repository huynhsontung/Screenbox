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

        #region PlayButton
        /// <summary>
        ///   Looks up a localized string similar to: Play
        /// </summary>
        public static string PlayButton
        {
            get
            {
                return _resourceLoader.GetString("PlayButton");
            }
        }
        #endregion

        #region PauseButton
        /// <summary>
        ///   Looks up a localized string similar to: Pause
        /// </summary>
        public static string PauseButton
        {
            get
            {
                return _resourceLoader.GetString("PauseButton");
            }
        }
        #endregion

        #region MuteButton
        /// <summary>
        ///   Looks up a localized string similar to: Mute
        /// </summary>
        public static string MuteButton
        {
            get
            {
                return _resourceLoader.GetString("MuteButton");
            }
        }
        #endregion

        #region UnmuteButton
        /// <summary>
        ///   Looks up a localized string similar to: Unmute
        /// </summary>
        public static string UnmuteButton
        {
            get
            {
                return _resourceLoader.GetString("UnmuteButton");
            }
        }
        #endregion

        #region EnterFullscreenButton
        /// <summary>
        ///   Looks up a localized string similar to: Fullscreen
        /// </summary>
        public static string EnterFullscreenButton
        {
            get
            {
                return _resourceLoader.GetString("EnterFullscreenButton");
            }
        }
        #endregion

        #region AudioAndCaptionButton
        /// <summary>
        ///   Looks up a localized string similar to: Audio & captions
        /// </summary>
        public static string AudioAndCaptionButton
        {
            get
            {
                return _resourceLoader.GetString("AudioAndCaptionButton");
            }
        }
        #endregion

        #region ExitFullscreenButton
        /// <summary>
        ///   Looks up a localized string similar to: Exit fullscreen
        /// </summary>
        public static string ExitFullscreenButton
        {
            get
            {
                return _resourceLoader.GetString("ExitFullscreenButton");
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
            FrameSavedNotificationTitle,
            VolumeChangeStatusMessage,
            FailedToSaveFrameNotificationTitle,
            ChapterName,
            FailedToLoadSubtitleNotificationTitle,
            PlayButton,
            PauseButton,
            MuteButton,
            UnmuteButton,
            EnterFullscreenButton,
            AudioAndCaptionButton,
            ExitFullscreenButton,
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

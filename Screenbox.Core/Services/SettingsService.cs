#nullable enable

using System;
using System.Linq;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Storage;

namespace Screenbox.Core.Services
{
    public sealed class SettingsService : ISettingsService
    {
        private static IPropertySet SettingsStorage => ApplicationData.Current.LocalSettings.Values;

        private const string GeneralThemeKey = "General/Theme";
        private const string PlayerAutoResizeKey = "Player/AutoResize";
        private const string PlayerShowControlsKey = "Player/ShowControls";
        private const string PlayerControlsHideDelayKey = "Player/ControlsHideDelay";
        private const string PlayerLivelyPathKey = "Player/Lively/Path";
        private const string LibrariesUseIndexerKey = "Libraries/UseIndexer";
        private const string LibrariesSearchRemovableStorageKey = "Libraries/SearchRemovableStorage";
        private const string GeneralShowRecent = "General/ShowRecent";
        private const string GeneralEnqueueAllInFolder = "General/EnqueueAllInFolder";
        private const string GeneralRestorePlaybackPosition = "General/RestorePlaybackPosition";
        private const string AdvancedModeKey = "Advanced/IsEnabled";
        private const string AdvancedVideoUpscaleKey = "Advanced/VideoUpscale";
        private const string AdvancedMultipleInstancesKey = "Advanced/MultipleInstances";
        private const string GlobalArgumentsKey = "Values/GlobalArguments";
        private const string PersistentVolumeKey = "Values/Volume";
        private const string MaxVolumeKey = "Values/MaxVolume";
        private const string PersistentRepeatModeKey = "Values/RepeatMode";
        private const string PersistentSubtitleLanguageKey = "Values/SubtitleLanguage";
        private const string PlayerShowChaptersKey = "Player/ShowChapters";

        private const string PlayerGestureTapKey = "Player/Gesture/Tap";
        private const string PlayerGestureSwipeUpKey = "Player/Gesture/SwipeUp";
        private const string PlayerGestureSwipeDownKey = "Player/Gesture/SwipeDown";
        private const string PlayerGestureSwipeLeftKey = "Player/Gesture/SwipeLeft";
        private const string PlayerGestureSwipeRightKey = "Player/Gesture/SwipeRight";

        public bool UseIndexer
        {
            get => GetValue<bool>(LibrariesUseIndexerKey);
            set => SetValue(LibrariesUseIndexerKey, value);
        }

        public ThemeOption Theme
        {
            get => (ThemeOption)GetValue<int>(GeneralThemeKey);
            set => SetValue(GeneralThemeKey, (int)value);
        }

        public PlayerAutoResizeOption PlayerAutoResize
        {
            get => (PlayerAutoResizeOption)GetValue<int>(PlayerAutoResizeKey);
            set => SetValue(PlayerAutoResizeKey, (int)value);
        }

        public int PersistentVolume
        {
            get => GetValue<int>(PersistentVolumeKey);
            set => SetValue(PersistentVolumeKey, value);
        }

        public string PersistentSubtitleLanguage
        {
            get => GetValue<string>(PersistentSubtitleLanguageKey) ?? string.Empty;
            set => SetValue(PersistentSubtitleLanguageKey, value);
        }

        public int MaxVolume
        {
            get => GetValue<int>(MaxVolumeKey);
            set => SetValue(MaxVolumeKey, value);
        }

        public bool ShowRecent
        {
            get => GetValue<bool>(GeneralShowRecent);
            set => SetValue(GeneralShowRecent, value);
        }

        public bool EnqueueAllFilesInFolder
        {
            get => GetValue<bool>(GeneralEnqueueAllInFolder);
            set => SetValue(GeneralEnqueueAllInFolder, value);
        }

        public bool RestorePlaybackPosition
        {
            get => GetValue<bool>(GeneralRestorePlaybackPosition);
            set => SetValue(GeneralRestorePlaybackPosition, value);
        }

        public bool PlayerShowControls
        {
            get => GetValue<bool>(PlayerShowControlsKey);
            set => SetValue(PlayerShowControlsKey, value);
        }

        public int PlayerControlsHideDelay
        {
            get => GetValue<int>(PlayerControlsHideDelayKey);
            set => SetValue(PlayerControlsHideDelayKey, value);
        }

        public bool SearchRemovableStorage
        {
            get => GetValue<bool>(LibrariesSearchRemovableStorageKey);
            set => SetValue(LibrariesSearchRemovableStorageKey, value);
        }

        public MediaPlaybackAutoRepeatMode PersistentRepeatMode
        {
            get => (MediaPlaybackAutoRepeatMode)GetValue<int>(PersistentRepeatModeKey);
            set => SetValue(PersistentRepeatModeKey, (int)value);
        }

        public string GlobalArguments
        {
            get => GetValue<string>(GlobalArgumentsKey) ?? string.Empty;
            set => SetValue(GlobalArgumentsKey, SanitizeArguments(value));
        }

        public bool AdvancedMode
        {
            get => GetValue<bool>(AdvancedModeKey);
            set => SetValue(AdvancedModeKey, value);
        }

        public VideoUpscaleOption VideoUpscale
        {
            get => (VideoUpscaleOption)GetValue<int>(AdvancedVideoUpscaleKey);
            set => SetValue(AdvancedVideoUpscaleKey, (int)value);
        }

        public bool UseMultipleInstances
        {
            get => GetValue<bool>(AdvancedMultipleInstancesKey);
            set => SetValue(AdvancedMultipleInstancesKey, value);
        }

        public string LivelyActivePath
        {
            get => GetValue<string>(PlayerLivelyPathKey) ?? string.Empty;
            set => SetValue(PlayerLivelyPathKey, value);
        }

        public bool PlayerShowChapters
        {
            get => GetValue<bool>(PlayerShowChaptersKey);
            set => SetValue(PlayerShowChaptersKey, value);
        }

        public MediaCommandType PlayerTapGesture
        {
            get => (MediaCommandType)GetValue<int>(PlayerGestureTapKey);
            set => SetValue(PlayerGestureTapKey, (int)value);
        }

        public MediaCommandType PlayerSwipeUpGesture
        {
            get => (MediaCommandType)GetValue<int>(PlayerGestureSwipeUpKey);
            set => SetValue(PlayerGestureSwipeUpKey, (int)value);
        }

        public MediaCommandType PlayerSwipeDownGesture
        {
            get => (MediaCommandType)GetValue<int>(PlayerGestureSwipeDownKey);
            set => SetValue(PlayerGestureSwipeDownKey, (int)value);
        }

        public MediaCommandType PlayerSwipeLeftGesture
        {
            get => (MediaCommandType)GetValue<int>(PlayerGestureSwipeLeftKey);
            set => SetValue(PlayerGestureSwipeLeftKey, (int)value);
        }

        public MediaCommandType PlayerSwipeRightGesture
        {
            get => (MediaCommandType)GetValue<int>(PlayerGestureSwipeRightKey);
            set => SetValue(PlayerGestureSwipeRightKey, (int)value);
        }

        public SettingsService()
        {
            SetDefault(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
            SetDefault(PlayerShowControlsKey, true);
            SetDefault(PlayerControlsHideDelayKey, 3);
            SetDefault(PersistentVolumeKey, 100);
            SetDefault(MaxVolumeKey, 100);
            SetDefault(LibrariesUseIndexerKey, true);
            SetDefault(LibrariesSearchRemovableStorageKey, true);
            SetDefault(GeneralShowRecent, true);
            SetDefault(PersistentRepeatModeKey, (int)MediaPlaybackAutoRepeatMode.None);
            SetDefault(AdvancedModeKey, false);
            SetDefault(AdvancedVideoUpscaleKey, (int)VideoUpscaleOption.Linear);
            SetDefault(AdvancedMultipleInstancesKey, false);
            SetDefault(GlobalArgumentsKey, string.Empty);
            SetDefault(PlayerShowChaptersKey, true);
            SetDefault(PlayerGestureTapKey, (int)MediaCommandType.PlayPause);
            SetDefault(PlayerGestureSwipeUpKey, (int)MediaCommandType.IncreaseVolume);
            SetDefault(PlayerGestureSwipeDownKey, (int)MediaCommandType.DecreaseVolume);
            SetDefault(PlayerGestureSwipeLeftKey, (int)MediaCommandType.Rewind);
            SetDefault(PlayerGestureSwipeRightKey, (int)MediaCommandType.FastForward);

            // Device family specific overrides
            if (SystemInformation.IsXbox)
            {
                SetValue(PlayerShowControlsKey, true);
                SetValue(PlayerGestureTapKey, (int)MediaCommandType.None);
                SetValue(PlayerGestureSwipeUpKey, (int)MediaCommandType.None);
                SetValue(PlayerGestureSwipeDownKey, (int)MediaCommandType.None);
                SetValue(PlayerGestureSwipeLeftKey, (int)MediaCommandType.None);
                SetValue(PlayerGestureSwipeRightKey, (int)MediaCommandType.None);
                SetValue(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
            }
        }

        private static T? GetValue<T>(string key)
        {
            if (SettingsStorage.TryGetValue(key, out object value))
            {
                return (T)value;
            }

            return default;
        }

        private static void SetValue<T>(string key, T value)
        {
            SettingsStorage[key] = value;
        }

        private static void SetDefault<T>(string key, T value)
        {
            if (SettingsStorage.ContainsKey(key) && SettingsStorage[key] is T) return;
            SettingsStorage[key] = value;
        }

        private static string SanitizeArguments(string raw)
        {
            string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith('-') && s != "--").ToArray();
            return string.Join(' ', args);
        }
    }
}

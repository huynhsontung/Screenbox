#nullable enable

using System;
using System.Linq;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Storage;
using Windows.UI.ViewManagement;

namespace Screenbox.Core.Services
{
    public sealed class SettingsService : ISettingsService
    {
        private static IPropertySet SettingsStorage => ApplicationData.Current.LocalSettings.Values;

        private const string GeneralThemeKey = "General/Theme";
        private const string PlayerAutoResizeKey = "Player/AutoResize";
        private const string PlayerVolumeGestureKey = "Player/Gesture/Volume";
        private const string PlayerSeekGestureKey = "Player/Gesture/Seek";
        private const string PlayerTapGestureKey = "Player/Gesture/Tap";
        private const string PlayerShowControlsKey = "Player/ShowControls";
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
        private const string PlayerAutoFullScreenKey = "Player/AutoFullScreen";

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

        public bool PlayerVolumeGesture
        {
            get => GetValue<bool>(PlayerVolumeGestureKey);
            set => SetValue(PlayerVolumeGestureKey, value);
        }

        public bool PlayerSeekGesture
        {
            get => GetValue<bool>(PlayerSeekGestureKey);
            set => SetValue(PlayerSeekGestureKey, value);
        }

        public bool PlayerTapGesture
        {
            get => GetValue<bool>(PlayerTapGestureKey);
            set => SetValue(PlayerTapGestureKey, value);
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

        public bool PlayerAutoFullScreen
        {
            get => GetValue<bool>(PlayerAutoFullScreenKey);
            set => SetValue(PlayerAutoFullScreenKey, value);
        }

        public SettingsService()
        {
            SetDefault(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
            SetDefault(PlayerVolumeGestureKey, true);
            SetDefault(PlayerSeekGestureKey, true);
            SetDefault(PlayerTapGestureKey, true);
            SetDefault(PlayerShowControlsKey, true);
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
            SetDefault(PlayerAutoFullScreenKey, false);

            // Device family specific overrides
            if (SystemInformation.IsXbox)
            {
                SetValue(PlayerTapGestureKey, false);
                SetValue(PlayerSeekGestureKey, false);
                SetValue(PlayerVolumeGestureKey, false);
                SetValue(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
                SetValue(PlayerShowControlsKey, true);
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

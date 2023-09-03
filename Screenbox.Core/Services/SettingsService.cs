#nullable enable

using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using System;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Storage;

namespace Screenbox.Core.Services
{
    public sealed class SettingsService : ISettingsService
    {
        private readonly IPropertySet _settingsStorage = ApplicationData.Current.LocalSettings.Values;

        private const string PlayerAutoResizeKey = "Player/AutoResize";
        private const string PlayerVolumeGestureKey = "Player/Gesture/Volume";
        private const string PlayerSeekGestureKey = "Player/Gesture/Seek";
        private const string PlayerTapGestureKey = "Player/Gesture/Tap";
        private const string GeneralShowRecent = "General/ShowRecent";
        private const string AdvancedModeKey = "Advanced/IsEnabled";
        private const string GlobalArgumentsKey = "Values/GlobalArguments";
        private const string PersistentVolumeKey = "Values/Volume";
        private const string MaxVolumeKey = "Values/MaxVolume";
        private const string PersistentRepeatModeKey = "Values/RepeatMode";

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

        public SettingsService()
        {
            SetDefault(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Always);
            SetDefault(PlayerVolumeGestureKey, true);
            SetDefault(PlayerSeekGestureKey, true);
            SetDefault(PlayerTapGestureKey, true);
            SetDefault(PersistentVolumeKey, 100);
            SetDefault(MaxVolumeKey, 100);
            SetDefault(GeneralShowRecent, true);
            SetDefault(PersistentRepeatModeKey, (int)MediaPlaybackAutoRepeatMode.None);
            SetDefault(AdvancedModeKey, false);
            SetDefault(GlobalArgumentsKey, string.Empty);

            // Device family specific overrides
            if (SystemInformationExtensions.IsXbox)
            {
                SetValue(PlayerTapGestureKey, false);
                SetValue(PlayerSeekGestureKey, false);
                SetValue(PlayerVolumeGestureKey, false);
                SetValue(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
            }
        }

        private T? GetValue<T>(string key)
        {
            if (_settingsStorage.TryGetValue(key, out object value))
            {
                return (T)value;
            }

            return default;
        }

        private void SetValue<T>(string key, T value)
        {
            _settingsStorage[key] = value;
        }

        private void SetDefault<T>(string key, T value)
        {
            if (_settingsStorage.ContainsKey(key) && _settingsStorage[key] is T) return;
            _settingsStorage[key] = value;
        }

        private static string SanitizeArguments(string raw)
        {
            string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith('-') && s != "--").ToArray();
            return string.Join(' ', args);
        }
    }
}

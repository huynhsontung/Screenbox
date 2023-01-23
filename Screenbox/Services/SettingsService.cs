#nullable enable

using Windows.Foundation.Collections;
using Windows.Storage;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal sealed class SettingsService : ISettingsService
    {
        private readonly IPropertySet _settingsStorage = ApplicationData.Current.LocalSettings.Values;

        private const string LibraryShowVideoFoldersKey = "Libraries/ShowVideoFolders";
        private const string PlayerAutoResizeKey = "Player/AutoResize";
        private const string PlayerVolumeGestureKey = "Player/Gesture/Volume";
        private const string PlayerSeekGestureKey = "Player/Gesture/Seek";
        private const string PlayerTapGestureKey = "Player/Gesture/Tap";
        private const string GeneralShowRecent = "General/ShowRecent";
        private const string PersistentVolumeKey = "Values/Volume";
        private const string MaxVolumeKey = "Values/MaxVolume";

        public PlayerAutoResizeOptions PlayerAutoResize
        {
            get => (PlayerAutoResizeOptions)GetValue<int>(PlayerAutoResizeKey);
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

        public bool ShowVideoFolders
        {
            get => GetValue<bool>(LibraryShowVideoFoldersKey);
            set => SetValue(LibraryShowVideoFoldersKey, value);
        }

        public bool ShowRecent
        {
            get => GetValue<bool>(GeneralShowRecent);
            set => SetValue(GeneralShowRecent, value);
        }

        public SettingsService()
        {
            SetDefault(PlayerAutoResizeKey, 0);
            SetDefault(PlayerVolumeGestureKey, true);
            SetDefault(PlayerSeekGestureKey, true);
            SetDefault(PlayerTapGestureKey, true);
            SetDefault(PersistentVolumeKey, 100);
            SetDefault(MaxVolumeKey, 100);
            SetDefault(LibraryShowVideoFoldersKey, true);
            SetDefault(GeneralShowRecent, true);
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
            if (!_settingsStorage.ContainsKey(key)) _settingsStorage.Add(key, value);
            else _settingsStorage[key] = value;
        }

        private void SetDefault<T>(string key, T value)
        {
            if (_settingsStorage.ContainsKey(key) && _settingsStorage[key] is T) return;
            _settingsStorage[key] = value;
        }
    }
}

#nullable enable

using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core;
using Screenbox.Core.Services;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Windows.System;
using Windows.Storage.AccessCache;

namespace Screenbox.ViewModels
{
    public sealed partial class SettingsPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private int _playerAutoResize;
        [ObservableProperty] private bool _playerVolumeGesture;
        [ObservableProperty] private bool _playerSeekGesture;
        [ObservableProperty] private bool _playerTapGesture;
        [ObservableProperty] private int _volumeBoost;
        [ObservableProperty] private bool _showRecent;

        public ObservableCollection<StorageFolder> MusicLocations { get; }

        public ObservableCollection<StorageFolder> VideoLocations { get; }

        private readonly ISettingsService _settingsService;
        private readonly DispatcherQueue _dispatcherQueue;
        private StorageLibrary? _videosLibrary;
        private StorageLibrary? _musicLibrary;

        public SettingsPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            MusicLocations = new ObservableCollection<StorageFolder>();
            VideoLocations = new ObservableCollection<StorageFolder>();

            LoadValues();
            LoadLibraryLocations();

            IsActive = true;
        }

        partial void OnPlayerAutoResizeChanged(int value)
        {
            _settingsService.PlayerAutoResize = (PlayerAutoResizeOption)value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerAutoResize)));
        }

        partial void OnPlayerVolumeGestureChanged(bool value)
        {
            _settingsService.PlayerVolumeGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerVolumeGesture)));
        }

        partial void OnPlayerSeekGestureChanged(bool value)
        {
            _settingsService.PlayerSeekGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerSeekGesture)));
        }

        partial void OnPlayerTapGestureChanged(bool value)
        {
            _settingsService.PlayerTapGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerTapGesture)));
        }

        partial void OnShowRecentChanged(bool value)
        {
            _settingsService.ShowRecent = value;
            Messenger.Send(new SettingsChangedMessage(nameof(ShowRecent)));
        }

        partial void OnVolumeBoostChanged(int value)
        {
            _settingsService.MaxVolume = value switch
            {
                3 => 200,
                2 => 150,
                1 => 125,
                _ => 100
            };
            Messenger.Send(new SettingsChangedMessage(nameof(VolumeBoost)));
        }

        [RelayCommand]
        private async Task AddVideosFolderAsync()
        {
            if (_videosLibrary == null) return;
            await _videosLibrary.RequestAddFolderAsync();
        }

        [RelayCommand]
        private async Task RemoveVideosFolderAsync(StorageFolder folder)
        {
            if (_videosLibrary == null) return;
            await _videosLibrary.RequestRemoveFolderAsync(folder);
        }

        [RelayCommand]
        private async Task AddMusicFolderAsync()
        {
            if (_musicLibrary == null) return;
            await _musicLibrary.RequestAddFolderAsync();
        }

        [RelayCommand]
        private async Task RemoveMusicFolderAsync(StorageFolder folder)
        {
            if (_musicLibrary == null) return;
            await _musicLibrary.RequestRemoveFolderAsync(folder);
        }

        [RelayCommand]
        private void ClearRecentHistory()
        {
            StorageApplicationPermissions.MostRecentlyUsedList.Clear();
        }

        private void LoadValues()
        {
            PlayerAutoResize = (int)_settingsService.PlayerAutoResize;
            PlayerVolumeGesture = _settingsService.PlayerVolumeGesture;
            PlayerSeekGesture = _settingsService.PlayerSeekGesture;
            PlayerTapGesture = _settingsService.PlayerTapGesture;
            ShowRecent = _settingsService.ShowRecent;
            int maxVolume = _settingsService.MaxVolume;
            VolumeBoost = maxVolume switch
            {
                >= 200 => 3,
                >= 150 => 2,
                >= 125 => 1,
                _ => 0
            };
        }

        private async void LoadLibraryLocations()
        {
            if (_videosLibrary != null || _musicLibrary != null) return;
            _videosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            _musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            UpdateLibraryLocations();
            _videosLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
            _musicLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
        }

        private void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateLibraryLocations);
        }

        private void UpdateLibraryLocations()
        {
            if (_videosLibrary == null || _musicLibrary == null) return;
            VideoLocations.Clear();
            MusicLocations.Clear();

            foreach (StorageFolder folder in _musicLibrary.Folders)
            {
                MusicLocations.Add(folder);
            }

            foreach (StorageFolder folder in _videosLibrary.Folders)
            {
                VideoLocations.Add(folder);
            }
        }
    }
}

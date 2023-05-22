#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;

namespace Screenbox.Core.ViewModels
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
        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private StorageLibrary? _videosLibrary;
        private StorageLibrary? _musicLibrary;

        public SettingsPageViewModel(ISettingsService settingsService, ILibraryService libraryService)
        {
            _settingsService = settingsService;
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            MusicLocations = new ObservableCollection<StorageFolder>();
            VideoLocations = new ObservableCollection<StorageFolder>();

            LoadValues();

            IsActive = true;
        }

        partial void OnPlayerAutoResizeChanged(int value)
        {
            _settingsService.PlayerAutoResize = (PlayerAutoResizeOption)value;
            Messenger.Send(new SettingsChangedMessage(nameof(SettingsPageViewModel.PlayerAutoResize)));
        }

        partial void OnPlayerVolumeGestureChanged(bool value)
        {
            _settingsService.PlayerVolumeGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(SettingsPageViewModel.PlayerVolumeGesture)));
        }

        partial void OnPlayerSeekGestureChanged(bool value)
        {
            _settingsService.PlayerSeekGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(SettingsPageViewModel.PlayerSeekGesture)));
        }

        partial void OnPlayerTapGestureChanged(bool value)
        {
            _settingsService.PlayerTapGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(SettingsPageViewModel.PlayerTapGesture)));
        }

        partial void OnShowRecentChanged(bool value)
        {
            _settingsService.ShowRecent = value;
            Messenger.Send(new SettingsChangedMessage(nameof(SettingsPageViewModel.ShowRecent)));
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
            Messenger.Send(new SettingsChangedMessage(nameof(SettingsPageViewModel.VolumeBoost)));
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

        public async Task LoadLibraryLocations()
        {
            if (_videosLibrary == null)
            {
                if (_libraryService.VideosLibrary == null)
                {
                    try
                    {
                        await _libraryService.InitializeVideosLibraryAsync();
                    }
                    catch (Exception)
                    {
                        // pass
                    }
                }

                _videosLibrary = _libraryService.VideosLibrary;
                if (_videosLibrary != null)
                {
                    _videosLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
                }
            }

            if (_musicLibrary == null)
            {
                if (_libraryService.MusicLibrary == null)
                {
                    try
                    {
                        await _libraryService.InitializeMusicLibraryAsync();
                    }
                    catch (Exception)
                    {
                        // pass
                    }
                }

                _musicLibrary = _libraryService.MusicLibrary;
                if (_musicLibrary != null)
                {
                    _musicLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
                }
            }

            UpdateLibraryLocations();
        }

        private void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateLibraryLocations);
        }

        private void UpdateLibraryLocations()
        {
            if (_videosLibrary != null)
            {
                VideoLocations.Clear();
                foreach (StorageFolder folder in _videosLibrary.Folders)
                {
                    VideoLocations.Add(folder);
                }
            }

            if (_musicLibrary != null)
            {
                MusicLocations.Clear();

                foreach (StorageFolder folder in _musicLibrary.Folders)
                {
                    MusicLocations.Add(folder);
                }
            }
        }
    }
}

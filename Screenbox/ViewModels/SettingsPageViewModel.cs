using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;
using Screenbox.Core;
using Screenbox.Services;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Windows.System;

namespace Screenbox.ViewModels
{
    internal sealed partial class SettingsPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        [ObservableProperty] private int _playerAutoResize;
        [ObservableProperty] private bool _playerVolumeGesture;
        [ObservableProperty] private bool _playerSeekGesture;
        [ObservableProperty] private bool _playerTapGesture;
        [ObservableProperty] private bool _showVideoFolders;

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
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();

            LoadValues();
            LoadLibraryLocations();

            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
        }

        partial void OnPlayerAutoResizeChanged(int value)
        {
            _settingsService.PlayerAutoResize = (PlayerAutoResizeOptions)value;
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

        partial void OnShowVideoFoldersChanged(bool value)
        {
            _settingsService.ShowVideoFolders = value;
            Messenger.Send(new SettingsChangedMessage(nameof(ShowVideoFolders)));
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

        private void LoadValues()
        {
            _playerAutoResize = (int)_settingsService.PlayerAutoResize;
            _playerVolumeGesture = _settingsService.PlayerVolumeGesture;
            _playerSeekGesture = _settingsService.PlayerSeekGesture;
            _playerTapGesture = _settingsService.PlayerTapGesture;
            _showVideoFolders = _settingsService.ShowVideoFolders;
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

using System;
using Windows.Foundation.Collections;
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
        [ObservableProperty] private IObservableVector<StorageFolder>? _musicLocations;
        [ObservableProperty] private IObservableVector<StorageFolder>? _videoLocations;
        [ObservableProperty] private int _musicLocationCount;   // Required for description due to weird binding behavior
        [ObservableProperty] private int _videoLocationCount;   // Required for description due to weird binding behavior

        private readonly ISettingsService _settingsService;
        private StorageLibrary? _videosLibrary;
        private StorageLibrary? _musicLibrary;

        public SettingsPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
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
        }

        private async void LoadLibraryLocations()
        {
            _videosLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            _musicLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);

            if (VideoLocations != null)
            {
                VideoLocations.VectorChanged -= LibraryLocationsOnVectorChanged;
            }

            if (MusicLocations != null)
            {
                MusicLocations.VectorChanged -= LibraryLocationsOnVectorChanged;
            }

            VideoLocations = _videosLibrary.Folders;
            MusicLocations = _musicLibrary.Folders;
            VideoLocations.VectorChanged += LibraryLocationsOnVectorChanged;
            MusicLocations.VectorChanged += LibraryLocationsOnVectorChanged;
            VideoLocationCount = VideoLocations.Count;
            MusicLocationCount = MusicLocations.Count;
        }

        private void LibraryLocationsOnVectorChanged(IObservableVector<StorageFolder> sender, IVectorChangedEventArgs _)
        {
            VideoLocationCount = VideoLocations?.Count ?? 0;
            MusicLocationCount = MusicLocations?.Count ?? 0;
        }
    }
}

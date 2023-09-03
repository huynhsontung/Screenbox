﻿#nullable enable

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
        [ObservableProperty] private bool _advancedMode;
        [ObservableProperty] private string _globalArguments;
        [ObservableProperty] private bool _isRelaunchRequired;

        public ObservableCollection<StorageFolder> MusicLocations { get; }

        public ObservableCollection<StorageFolder> VideoLocations { get; }

        private readonly ISettingsService _settingsService;
        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _argumentsChangedTimer;
        private static string? _originalGlobalArguments;
        private static bool? _originalAdvancedMode;
        private StorageLibrary? _videosLibrary;
        private StorageLibrary? _musicLibrary;

        public SettingsPageViewModel(ISettingsService settingsService, ILibraryService libraryService)
        {
            _settingsService = settingsService;
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _argumentsChangedTimer = _dispatcherQueue.CreateTimer();
            MusicLocations = new ObservableCollection<StorageFolder>();
            VideoLocations = new ObservableCollection<StorageFolder>();

            // Load values
            _playerAutoResize = (int)_settingsService.PlayerAutoResize;
            _playerVolumeGesture = _settingsService.PlayerVolumeGesture;
            _playerSeekGesture = _settingsService.PlayerSeekGesture;
            _playerTapGesture = _settingsService.PlayerTapGesture;
            _showRecent = _settingsService.ShowRecent;
            _advancedMode = _settingsService.AdvancedMode;
            _globalArguments = _settingsService.GlobalArguments;
            _originalAdvancedMode ??= _advancedMode;
            _originalGlobalArguments ??= _globalArguments;
            _isRelaunchRequired = CheckForRelaunch();
            int maxVolume = _settingsService.MaxVolume;
            _volumeBoost = maxVolume switch
            {
                >= 200 => 3,
                >= 150 => 2,
                >= 125 => 1,
                _ => 0
            };

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

        partial void OnAdvancedModeChanged(bool value)
        {
            _settingsService.AdvancedMode = value;
            Messenger.Send(new SettingsChangedMessage(nameof(AdvancedMode)));
            IsRelaunchRequired = CheckForRelaunch();
        }

        partial void OnGlobalArgumentsChanged(string value)
        {
            // No need to broadcast SettingsChangedMessage for this option
            _settingsService.GlobalArguments = value;
            IsRelaunchRequired = CheckForRelaunch();
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
            try
            {
                await _videosLibrary.RequestRemoveFolderAsync(folder);
            }
            catch (Exception)
            {
                // System.Exception: The remote procedure call was cancelled.
                // pass
            }
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
            try
            {
                await _musicLibrary.RequestRemoveFolderAsync(folder);
            }
            catch (Exception)
            {
                // System.Exception: The remote procedure call was cancelled.
                // pass
            }
        }

        [RelayCommand]
        private void ClearRecentHistory()
        {
            StorageApplicationPermissions.MostRecentlyUsedList.Clear();
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

        private bool CheckForRelaunch()
        {
            // Check if global arguments have been changed
            bool argsChanged = _originalGlobalArguments != _settingsService.GlobalArguments;

            // Check if advanced mode has been changed
            bool modeChanged = _originalAdvancedMode != AdvancedMode;

            // Check if there are any global arguments set
            bool hasArgs = _settingsService.GlobalArguments.Length > 0;

            // Check if advanced mode is on, and if global arguments are set
            bool whenOn = modeChanged && AdvancedMode && hasArgs;

            // Check if advanced mode is off, and if global arguments are set or have been removed
            bool whenOff = modeChanged && !AdvancedMode && (!hasArgs && argsChanged || hasArgs);

            // Require relaunch when advanced mode is on and global arguments have been changed
            bool whenOnAndChanged = AdvancedMode && argsChanged;

            // Combine everything
            return whenOn || whenOff || whenOnAndChanged;
        }
    }
}

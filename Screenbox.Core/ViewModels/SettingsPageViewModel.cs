#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Devices.Enumeration;
using Windows.Globalization;
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
        [ObservableProperty] private bool _playerShowControls;
        [ObservableProperty] private bool _playerShowChapters;
        [ObservableProperty] private int _playerControlsHideDelay;
        [ObservableProperty] private int _volumeBoost;
        [ObservableProperty] private bool _useIndexer;
        [ObservableProperty] private bool _showRecent;
        [ObservableProperty] private int _theme;
        [ObservableProperty] private bool _enqueueAllFilesInFolder;
        [ObservableProperty] private bool _restorePlaybackPosition;
        [ObservableProperty] private bool _trackLastPosition;
        [ObservableProperty] private bool _searchRemovableStorage;
        [ObservableProperty] private bool _advancedMode;
        [ObservableProperty] private int _videoUpscaling;
        [ObservableProperty] private bool _useMultipleInstances;
        [ObservableProperty] private string _globalArguments;
        [ObservableProperty] private bool _isRelaunchRequired;
        [ObservableProperty] private int _selectedLanguage;

        public ObservableCollection<StorageFolder> MusicLocations { get; }

        public ObservableCollection<StorageFolder> VideoLocations { get; }

        public ObservableCollection<StorageFolder> RemovableStorageFolders { get; }

        public List<Models.Language> AvailableLanguages { get; }

        public int[] PlayerControlsHideDelayOptions { get; } = { 1, 2, 3, 4, 5 };

        private readonly ISettingsService _settingsService;
        private readonly ILibraryService _libraryService;
        private readonly LastPositionTracker _lastPositionTracker;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _storageDeviceRefreshTimer;
        private readonly DeviceWatcher? _portableStorageDeviceWatcher;
        private static InitialValues? _initialValues;
        private StorageLibrary? _videosLibrary;
        private StorageLibrary? _musicLibrary;

        private record InitialValues(string GlobalArguments, bool AdvancedMode, int VideoUpscaling, int Language)
        {
            public string GlobalArguments { get; } = GlobalArguments;
            public bool AdvancedMode { get; } = AdvancedMode;
            public int VideoUpscaling { get; } = VideoUpscaling;
            public int Language { get; } = Language;
        }

        public SettingsPageViewModel(ISettingsService settingsService, ILibraryService libraryService, LastPositionTracker lastPositionTracker)
        {
            _settingsService = settingsService;
            _libraryService = libraryService;
            _lastPositionTracker = lastPositionTracker;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _storageDeviceRefreshTimer = _dispatcherQueue.CreateTimer();
            MusicLocations = new ObservableCollection<StorageFolder>();
            VideoLocations = new ObservableCollection<StorageFolder>();
            RemovableStorageFolders = new ObservableCollection<StorageFolder>();

            var manifestLanguages = ApplicationLanguages.ManifestLanguages.Select(l => new Language(l)).ToList();
            AvailableLanguages = manifestLanguages.Select(l => new Models.Language(l.NativeName, l.LanguageTag, l.LayoutDirection))
                .OrderBy(l => l.NativeName, StringComparer.CurrentCultureIgnoreCase)
                .Prepend(new Models.Language(string.Empty, string.Empty, LanguageLayoutDirection.Ltr))
                .ToList();

            if (SystemInformation.IsXbox)
            {
                _portableStorageDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
                _portableStorageDeviceWatcher.Updated += OnPortableStorageDeviceChanged;
                _portableStorageDeviceWatcher.Removed += OnPortableStorageDeviceChanged;
                _portableStorageDeviceWatcher.Start();
            }

            // Load values
            _playerAutoResize = (int)_settingsService.PlayerAutoResize;
            _playerVolumeGesture = _settingsService.PlayerVolumeGesture;
            _playerSeekGesture = _settingsService.PlayerSeekGesture;
            _playerTapGesture = _settingsService.PlayerTapGesture;
            _playerShowControls = _settingsService.PlayerShowControls;
            _playerShowChapters = _settingsService.PlayerShowChapters;
            _playerControlsHideDelay = _settingsService.PlayerControlsHideDelay;
            _useIndexer = _settingsService.UseIndexer;
            _showRecent = _settingsService.ShowRecent;
            _theme = ((int)_settingsService.Theme + 2) % 3;
            _enqueueAllFilesInFolder = _settingsService.EnqueueAllFilesInFolder;
            _restorePlaybackPosition = _settingsService.RestorePlaybackPosition;
            _trackLastPosition = _settingsService.TrackLastPosition;
            _searchRemovableStorage = _settingsService.SearchRemovableStorage;
            _advancedMode = _settingsService.AdvancedMode;
            _useMultipleInstances = _settingsService.UseMultipleInstances;
            _videoUpscaling = (int)_settingsService.VideoUpscale;
            _globalArguments = _settingsService.GlobalArguments;
            int maxVolume = _settingsService.MaxVolume;
            _volumeBoost = maxVolume switch
            {
                >= 200 => 3,
                >= 150 => 2,
                >= 125 => 1,
                _ => 0
            };

            string currentLanguage = ApplicationLanguages.PrimaryLanguageOverride;
            _selectedLanguage = AvailableLanguages.FindIndex(l => l.LanguageTag.Equals(currentLanguage));

            // Setting initial values for relaunch check
            _initialValues ??= new InitialValues(_globalArguments, _advancedMode, _videoUpscaling, _selectedLanguage);
            CheckForRelaunch();

            IsActive = true;
        }

        partial void OnThemeChanged(int value)
        {
            // The recommended theme option order is Light, Dark, System
            // So we need to map the value to the correct ThemeOption
            _settingsService.Theme = (ThemeOption)((value + 1) % 3);
            Messenger.Send(new SettingsChangedMessage(nameof(Theme), typeof(SettingsPageViewModel)));
        }

        partial void OnSelectedLanguageChanged(int value)
        {
            if (value <= 0)
            {
                ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
                CheckForRelaunch();
                return;
            }

            // If the value is out of bounds, do nothing
            if (value >= AvailableLanguages.Count) return;
            ApplicationLanguages.PrimaryLanguageOverride = AvailableLanguages[value].LanguageTag;
            CheckForRelaunch();
        }

        partial void OnPlayerAutoResizeChanged(int value)
        {
            _settingsService.PlayerAutoResize = (PlayerAutoResizeOption)value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerAutoResize), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerVolumeGestureChanged(bool value)
        {
            _settingsService.PlayerVolumeGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerVolumeGesture), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerSeekGestureChanged(bool value)
        {
            _settingsService.PlayerSeekGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerSeekGesture), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerTapGestureChanged(bool value)
        {
            _settingsService.PlayerTapGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerTapGesture), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerShowControlsChanged(bool value)
        {
            _settingsService.PlayerShowControls = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerShowControls), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerShowChaptersChanged(bool value)
        {
            _settingsService.PlayerShowChapters = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerShowChapters), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerControlsHideDelayChanged(int value)
        {
            _settingsService.PlayerControlsHideDelay = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerControlsHideDelay), typeof(SettingsPageViewModel)));
        }

        partial void OnUseIndexerChanged(bool value)
        {
            _settingsService.UseIndexer = value;
            Messenger.Send(new SettingsChangedMessage(nameof(UseIndexer), typeof(SettingsPageViewModel)));
        }

        partial void OnShowRecentChanged(bool value)
        {
            _settingsService.ShowRecent = value;
            Messenger.Send(new SettingsChangedMessage(nameof(ShowRecent), typeof(SettingsPageViewModel)));
        }

        partial void OnEnqueueAllFilesInFolderChanged(bool value)
        {
            _settingsService.EnqueueAllFilesInFolder = value;
            Messenger.Send(new SettingsChangedMessage(nameof(EnqueueAllFilesInFolder), typeof(SettingsPageViewModel)));
        }

        partial void OnRestorePlaybackPositionChanged(bool value)
        {
            _settingsService.RestorePlaybackPosition = value;
            Messenger.Send(new SettingsChangedMessage(nameof(RestorePlaybackPosition), typeof(SettingsPageViewModel)));
        }

        partial void OnTrackLastPositionChanged(bool value)
        {
            _settingsService.TrackLastPosition = value;
            Messenger.Send(new SettingsChangedMessage(nameof(TrackLastPosition), typeof(SettingsPageViewModel)));
        }

        async partial void OnSearchRemovableStorageChanged(bool value)
        {
            _settingsService.SearchRemovableStorage = value;
            Messenger.Send(new SettingsChangedMessage(nameof(SearchRemovableStorage), typeof(SettingsPageViewModel)));

            if (SystemInformation.IsXbox && RemovableStorageFolders.Count > 0)
            {
                await RefreshLibrariesAsync();
            }
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
            Messenger.Send(new SettingsChangedMessage(nameof(VolumeBoost), typeof(SettingsPageViewModel)));
        }

        partial void OnAdvancedModeChanged(bool value)
        {
            _settingsService.AdvancedMode = value;
            Messenger.Send(new SettingsChangedMessage(nameof(AdvancedMode), typeof(SettingsPageViewModel)));
            CheckForRelaunch();
        }

        partial void OnVideoUpscalingChanged(int value)
        {
            _settingsService.VideoUpscale = (VideoUpscaleOption)value;
            Messenger.Send(new SettingsChangedMessage(nameof(VideoUpscaling), typeof(SettingsPageViewModel)));
            CheckForRelaunch();
        }

        partial void OnUseMultipleInstancesChanged(bool value)
        {
            _settingsService.UseMultipleInstances = value;
            Messenger.Send(new SettingsChangedMessage(nameof(UseMultipleInstances), typeof(SettingsPageViewModel)));
        }

        partial void OnGlobalArgumentsChanged(string value)
        {
            // No need to broadcast SettingsChangedMessage for this option
            if (value != _settingsService.GlobalArguments)
            {
                _settingsService.GlobalArguments = value;
            }

            GlobalArguments = _settingsService.GlobalArguments;
            CheckForRelaunch();
        }

        [RelayCommand]
        private async Task RefreshLibrariesAsync()
        {
            await Task.WhenAll(RefreshMusicLibrary(), RefreshVideosLibrary());
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

        [RelayCommand]
        private async Task ClearLastPositionHistoryAsync()
        {
            _lastPositionTracker.ClearAll();
            await _lastPositionTracker.SaveToDiskAsync();
        }

        public void OnNavigatedFrom()
        {
            if (SystemInformation.IsXbox)
                _portableStorageDeviceWatcher?.Stop();
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
            await UpdateRemovableStorageFoldersAsync();
        }

        private void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateLibraryLocations);
        }

        private void OnPortableStorageDeviceChanged(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            async void RefreshAction() => await UpdateRemovableStorageFoldersAsync();
            _storageDeviceRefreshTimer.Debounce(RefreshAction, TimeSpan.FromMilliseconds(500));
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

        private async Task UpdateRemovableStorageFoldersAsync()
        {
            if (SystemInformation.IsXbox)
            {
                RemovableStorageFolders.Clear();
                var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                if (accessStatus != KnownFoldersAccessStatus.Allowed)
                    return;

                foreach (StorageFolder folder in await KnownFolders.RemovableDevices.GetFoldersAsync())
                {
                    RemovableStorageFolders.Add(folder);
                }
            }
        }

        private async Task RefreshMusicLibrary()
        {
            try
            {
                await _libraryService.FetchMusicAsync(false);
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Music));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(null, e.Message));
                LogService.Log(e);
            }
        }

        private async Task RefreshVideosLibrary()
        {
            try
            {
                await _libraryService.FetchVideosAsync(false);
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Videos));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(null, e.Message));
                LogService.Log(e);
            }
        }

        private void CheckForRelaunch()
        {
            if (_initialValues == null) return;

            // Check if upscaling mode has been changed
            bool upscalingChanged = _initialValues.VideoUpscaling != VideoUpscaling;

            // Check if app language has been changed
            bool languageChanged = _initialValues.Language != SelectedLanguage;

            // Check if global arguments have been changed
            bool argsChanged = _initialValues.GlobalArguments != _settingsService.GlobalArguments;

            // Check if advanced mode has been changed
            bool modeChanged = _initialValues.AdvancedMode != AdvancedMode;

            // Check if there are any global arguments set
            bool hasArgs = _settingsService.GlobalArguments.Length > 0;

            // Check if advanced mode is on, and if global arguments are set
            bool whenOn = modeChanged && AdvancedMode && hasArgs;

            // Check if advanced mode is off, and if global arguments are set or have been removed
            bool whenOff = modeChanged && !AdvancedMode && (!hasArgs && argsChanged || hasArgs);

            // Require relaunch when advanced mode is on and global arguments have been changed
            bool whenOnAndChanged = AdvancedMode && argsChanged;

            // Combine everything
            IsRelaunchRequired = upscalingChanged || languageChanged || whenOn || whenOff || whenOnAndChanged;
        }
    }
}

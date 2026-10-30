using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.Xaml;

namespace Screenbox.Core.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableRecipient
{
    public PlayerSettingsContext PlayerSettings { get; }
    public LibrarySettingsContext LibrarySettings { get; }
    public PlaybackSettingsContext PlaybackSettings { get; }
    public GeneralSettingsContext GeneralSettings { get; }

    [ObservableProperty] public partial string Language { get; set; }
    [ObservableProperty] public partial bool ShowRecent { get; set; }
    [ObservableProperty] public partial bool SearchRemovableStorage { get; set; }
    [ObservableProperty] public partial bool AdvancedMode { get; set; }
    [ObservableProperty] public partial int VideoUpscaling { get; set; }
    [ObservableProperty] public partial string GlobalArguments { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsRelaunchRequired { get; set; }
    [ObservableProperty] public partial StorageFolder? FrameCaptureFolder { get; set; }

    public ObservableCollection<StorageFolder> MusicLocations { get; }

    public ObservableCollection<StorageFolder> VideoLocations { get; }

    public ObservableCollection<StorageFolder> RemovableStorageFolders { get; }

    public IReadOnlyList<PlayerAutoResizeOption> PlayerAutoResizeOptions { get; } = Enum.GetValues<PlayerAutoResizeOption>();

    public IReadOnlyList<int> VolumeBoostOptions { get; } = _volumeOptions;

    public IReadOnlyList<int> PlayerSeekStepOptions { get; } = new[] { 5, 10, 15, 20, 30 };

    public IReadOnlyList<int> PlayerControlsHideDelayOptions { get; } = new[] { 1, 2, 3, 4, 5 };

    public IReadOnlyList<PlaybackActionKind> GestureOptions { get; } = Enum.GetValues<PlaybackActionKind>();

    private static readonly int[] _volumeOptions = [100, 125, 150, 200];

    private readonly IFilesService _filesService;
    private readonly LibraryContext _libraryContext;
    private readonly RecentContext _recentContext;
    private readonly ILibraryCoordinator _libraryCoordinator;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _storageDeviceRefreshTimer;
    private readonly DeviceWatcher? _portableStorageDeviceWatcher;
    private readonly IPlaybackProgressTracker _playbackProgressTracker;
    private readonly ILogger<SettingsPageViewModel> _logger;
    private static InitialValues? _initialValues;
    private StorageLibrary? _videosLibrary;
    private StorageLibrary? _musicLibrary;

    private record InitialValues(string GlobalArguments, bool AdvancedMode, int VideoUpscaling, string Language)
    {
        public string GlobalArguments { get; } = GlobalArguments;
        public bool AdvancedMode { get; } = AdvancedMode;
        public int VideoUpscaling { get; } = VideoUpscaling;
        public string Language { get; } = Language;
    }

    public SettingsPageViewModel(
        PlayerSettingsContext playerSettings,
        LibrarySettingsContext librarySettings,
        PlaybackSettingsContext playbackSettings,
        GeneralSettingsContext generalSettings,
        IFilesService filesService,
        LibraryContext libraryContext,
        RecentContext recentContext,
        ILibraryCoordinator libraryCoordinator,
        IPlaybackProgressTracker playbackProgressTracker,
        ILogger<SettingsPageViewModel> logger)
    {
        PlayerSettings = playerSettings;
        LibrarySettings = librarySettings;
        PlaybackSettings = playbackSettings;
        GeneralSettings = generalSettings;
        _filesService = filesService;
        _libraryContext = libraryContext;
        _recentContext = recentContext;
        _libraryCoordinator = libraryCoordinator;
        _playbackProgressTracker = playbackProgressTracker;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _storageDeviceRefreshTimer = _dispatcherQueue.CreateTimer();
        MusicLocations = new ObservableCollection<StorageFolder>();
        VideoLocations = new ObservableCollection<StorageFolder>();
        RemovableStorageFolders = new ObservableCollection<StorageFolder>();

        if (SystemInformation.IsXbox)
        {
            _portableStorageDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
            _portableStorageDeviceWatcher.Updated += OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Removed += OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Start();
        }

        // Load values
        Language = GeneralSettings.Language;
        ShowRecent = GeneralSettings.ShowRecent;
        SearchRemovableStorage = LibrarySettings.SearchRemovableStorage;
        AdvancedMode = PlayerSettings.AdvancedMode;
        VideoUpscaling = (int)PlayerSettings.VideoUpscale;
        GlobalArguments = PlayerSettings.GlobalArguments;

        // Setting initial values for relaunch check
        _initialValues ??= new InitialValues(GlobalArguments, AdvancedMode, VideoUpscaling, Language);
        CheckForRelaunch();
    }

    partial void OnLanguageChanged(string value)
    {
        GeneralSettings.Language = value;
        CheckForRelaunch();
    }

    partial void OnShowRecentChanged(bool value)
    {
        GeneralSettings.ShowRecent = value;
        if (!value)
        {
            ClearRecentContext();
        }
    }

    partial void OnSearchRemovableStorageChanged(bool value)
    {
        LibrarySettings.SearchRemovableStorage = value;
        if (SystemInformation.IsXbox && RemovableStorageFolders.Count > 0)
        {
            _dispatcherQueue.TryEnqueue(async () => await RefreshLibrariesAsync());
        }
    }

    partial void OnAdvancedModeChanged(bool value)
    {
        PlayerSettings.AdvancedMode = value;
        CheckForRelaunch();
    }

    partial void OnVideoUpscalingChanged(int value)
    {
        PlayerSettings.VideoUpscale = (VideoUpscaleOption)value;
        CheckForRelaunch();
    }

    partial void OnGlobalArgumentsChanged(string value)
    {
        string sanitized = VlcArgumentHelper.SanitizeArguments(value);
        if (sanitized != value)
        {
            GlobalArguments = sanitized;
            return;
        }

        PlayerSettings.GlobalArguments = sanitized;
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
    private async Task ChangeFrameCaptureLocationAsync()
    {
        StorageFolder? folder = await _filesService.PickFolderAsync();
        if (folder is null) return;

        // Release the previous entry so the FutureAccessList (capped at 1000 items) doesn't accumulate stale folders.
        string oldToken = PlayerSettings.FrameCaptureFolderToken;
        if (!string.IsNullOrEmpty(oldToken) && StorageApplicationPermissions.FutureAccessList.ContainsItem(oldToken))
        {
            StorageApplicationPermissions.FutureAccessList.Remove(oldToken);
        }

        string newToken = StorageApplicationPermissions.FutureAccessList.Add(folder, "frame-capture");
        PlayerSettings.FrameCaptureFolderToken = newToken;
        FrameCaptureFolder = folder;
    }

    [RelayCommand]
    private async Task OpenFrameCaptureFolderAsync()
    {
        try
        {
            await LoadFrameCaptureFolderAsync();
            await Launcher.LaunchFolderAsync(FrameCaptureFolder);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to open the frame capture folder.");
        }
    }

    [RelayCommand]
    private void ClearRecentHistory()
    {
        ClearRecentContext();
        StorageApplicationPermissions.MostRecentlyUsedList.Clear();
    }

    private void ClearRecentContext()
    {
        _recentContext.Recent.Clear();
        _recentContext.PathToMruMappings.Clear();
        _recentContext.TokenToMediaMappings.Clear();
        _recentContext.IsLoaded = true;
    }

    [RelayCommand]
    private async Task ClearPlaybackPositionHistoryAsync()
    {
        try
        {
            _playbackProgressTracker.ClearAll();
            await _playbackProgressTracker.SaveToDiskAsync();
        }
        catch (Exception)
        {
            // pass
        }
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
            _videosLibrary = _libraryContext.VideosStorageLibrary;
            if (_videosLibrary != null)
            {
                _videosLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
            }
        }

        if (_musicLibrary == null)
        {
            _musicLibrary = _libraryContext.MusicStorageLibrary;
            if (_musicLibrary != null)
            {
                _musicLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
            }
        }

        UpdateLibraryLocations();
        await UpdateRemovableStorageFoldersAsync();
    }

    public async Task LoadFrameCaptureFolderAsync()
    {
        try
        {
            FrameCaptureFolder = await _filesService.GetFrameCaptureFolderAsync(PlayerSettings.FrameCaptureFolderToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load the frame capture folder.");
        }
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
            if (SystemInformation.IsKnownFolderRequestAccessSupported)
            {
                var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                if (accessStatus != KnownFoldersAccessStatus.Allowed)
                    return;
            }

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
            await _libraryCoordinator.FetchMusicAsync(false);
        }
        catch (UnauthorizedAccessException)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MusicLibraryAccessDenied));
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.None, message: e.Message));
            _logger.LogError(e, "Failed to refresh the music library from settings.");
        }
    }

    private async Task RefreshVideosLibrary()
    {
        try
        {
            await _libraryCoordinator.FetchVideosAsync(false);
        }
        catch (UnauthorizedAccessException)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.VideosLibraryAccessDenied));
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.None, message: e.Message));
            _logger.LogError(e, "Failed to refresh the videos library from settings.");
        }
    }

    private void CheckForRelaunch()
    {
        if (_initialValues == null) return;

        // Check if upscaling mode has been changed
        bool upscalingChanged = _initialValues.VideoUpscaling != VideoUpscaling;

        // Check if app language has been changed
        bool languageChanged = _initialValues.Language != Language;

        // Check if global arguments have been changed
        bool argsChanged = _initialValues.GlobalArguments != GlobalArguments;

        // Check if advanced mode has been changed
        bool modeChanged = _initialValues.AdvancedMode != AdvancedMode;

        // Check if there are any global arguments set
        bool hasArgs = GlobalArguments.Length > 0;

        // Check if advanced mode is on, and if global arguments are set
        bool whenOn = modeChanged && AdvancedMode && hasArgs;

        // Check if advanced mode is off, and if global arguments are set or have been removed
        bool whenOff = modeChanged && !AdvancedMode && ((!hasArgs && argsChanged) || hasArgs);

        // Require relaunch when advanced mode is on and global arguments have been changed
        bool whenOnAndChanged = AdvancedMode && argsChanged;

        // Combine everything
        IsRelaunchRequired = upscalingChanged || languageChanged || whenOn || whenOff || whenOnAndChanged;
    }
}

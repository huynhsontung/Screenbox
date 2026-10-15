using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Devices.Enumeration;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableRecipient
{
    public SettingsContext Settings { get; }
    public PlayerSettingsContext PlayerSettings => Settings.Player;
    public LibrarySettingsContext LibrarySettings => Settings.Library;
    public PlaybackSettingsContext PlaybackSettings => Settings.Playback;
    public GeneralSettingsContext GeneralSettings => Settings.General;
    public AdvancedSettingsContext AdvancedSettings => Settings.Advanced;
    public PrivacySettingsContext PrivacySettings => Settings.Privacy;
    public PersonalizationSettingsContext PersonalizationSettings => Settings.Personalization;

    public int Theme
    {
        get => ((int)PersonalizationSettings.Theme + 2) % 3;
        set => PersonalizationSettings.Theme = (ThemeOption)((value + 1) % 3);
    }

    public int VolumeBoost
    {
        get => PlaybackSettings.MaxVolume switch
        {
            >= 200 => 3,
            >= 150 => 2,
            >= 125 => 1,
            _ => 0
        };
        set => PlaybackSettings.MaxVolume = value switch
        {
            3 => 200,
            2 => 150,
            1 => 125,
            _ => 100
        };
    }

    public int PlayerAutoResize
    {
        get => (int)PlayerSettings.AutoResize;
        set => PlayerSettings.AutoResize = (PlayerAutoResizeOption)value;
    }

    public int VideoUpscaling
    {
        get => (int)AdvancedSettings.VideoUpscale;
        set => AdvancedSettings.VideoUpscale = (VideoUpscaleOption)value;
    }

    public string GlobalArguments
    {
        get => AdvancedSettings.GlobalArguments;
        set => AdvancedSettings.GlobalArguments = value;
    }

    public bool AdvancedMode
    {
        get => AdvancedSettings.AdvancedMode;
        set => AdvancedSettings.AdvancedMode = value;
    }

    public bool PlayerShowChapters
    {
        get => PlayerSettings.ShowChapters;
        set => PlayerSettings.ShowChapters = value;
    }

    [ObservableProperty] public partial bool IsRelaunchRequired { get; set; }
    [ObservableProperty] public partial int SelectedLanguage { get; set; }
    [ObservableProperty] public partial StorageFolder? FrameCaptureFolder { get; set; }

    public ObservableCollection<StorageFolder> MusicLocations { get; }
    public ObservableCollection<StorageFolder> VideoLocations { get; }
    public ObservableCollection<StorageFolder> RemovableStorageFolders { get; }
    public List<LanguageInfo> AvailableLanguages { get; }

    public IReadOnlyList<int> PlayerSeekStepOptions { get; } = new[] { 5, 10, 15, 20, 30 };
    public IReadOnlyList<int> PlayerControlsHideDelayOptions { get; } = new[] { 1, 2, 3, 4, 5 };
    public IReadOnlyList<PlaybackActionKind> GestureOptions { get; } = Enum.GetValues<PlaybackActionKind>();

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

    private record InitialValues(string GlobalArguments, bool AdvancedMode, int VideoUpscaling, int Language)
    {
        public string GlobalArguments { get; } = GlobalArguments;
        public bool AdvancedMode { get; } = AdvancedMode;
        public int VideoUpscaling { get; } = VideoUpscaling;
        public int Language { get; } = Language;
    }

    public SettingsPageViewModel(
        SettingsContext settingsContext,
        IFilesService filesService,
        LibraryContext libraryContext,
        RecentContext recentContext,
        ILibraryCoordinator libraryCoordinator,
        IPlaybackProgressTracker playbackProgressTracker,
        ILogger<SettingsPageViewModel> logger)
    {
        Settings = settingsContext;
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

        IEnumerable<Language> manifestLanguages = ApplicationLanguages.ManifestLanguages.Select(l => new Language(l));
        AvailableLanguages = manifestLanguages.Select(l => new LanguageInfo(l.NativeName, l.LanguageTag))
            .OrderBy(l => l.NativeName, StringComparer.CurrentCultureIgnoreCase)
            .Prepend(new LanguageInfo(string.Empty, string.Empty))
            .ToList();

        if (SystemInformation.IsXbox)
        {
            _portableStorageDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
            _portableStorageDeviceWatcher.Updated += OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Removed += OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Start();
        }

        string currentLanguage = ApplicationLanguages.PrimaryLanguageOverride;
        SelectedLanguage = AvailableLanguages.FindIndex(l => l.LanguageTag.Equals(currentLanguage));

        // Wire context property change listeners for UI notifications and relaunch checks
        GeneralSettings.PropertyChanged += OnGeneralSettingsPropertyChanged;
        PlaybackSettings.PropertyChanged += OnPlaybackSettingsPropertyChanged;
        PlayerSettings.PropertyChanged += OnPlayerSettingsPropertyChanged;
        LibrarySettings.PropertyChanged += OnLibrarySettingsPropertyChanged;
        AdvancedSettings.PropertyChanged += OnAdvancedSettingsPropertyChanged;
        PrivacySettings.PropertyChanged += OnPrivacySettingsPropertyChanged;
        PersonalizationSettings.PropertyChanged += OnPersonalizationSettingsPropertyChanged;

        // Setting initial values for relaunch check
        _initialValues ??= new InitialValues(AdvancedSettings.GlobalArguments, AdvancedSettings.AdvancedMode, VideoUpscaling, SelectedLanguage);
        CheckForRelaunch();
    }

    partial void OnSelectedLanguageChanged(int value)
    {
        if (value <= 0)
        {
            ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
            CheckForRelaunch();
            return;
        }

        if (value >= AvailableLanguages.Count) return;
        ApplicationLanguages.PrimaryLanguageOverride = AvailableLanguages[value].LanguageTag;
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
        }
    }

    [RelayCommand]
    private async Task ChangeFrameCaptureLocationAsync()
    {
        StorageFolder? folder = await _filesService.PickFolderAsync();
        if (folder is null) return;

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
        GeneralSettings.PropertyChanged -= OnGeneralSettingsPropertyChanged;
        PlaybackSettings.PropertyChanged -= OnPlaybackSettingsPropertyChanged;
        PlayerSettings.PropertyChanged -= OnPlayerSettingsPropertyChanged;
        LibrarySettings.PropertyChanged -= OnLibrarySettingsPropertyChanged;
        AdvancedSettings.PropertyChanged -= OnAdvancedSettingsPropertyChanged;
        PrivacySettings.PropertyChanged -= OnPrivacySettingsPropertyChanged;
        PersonalizationSettings.PropertyChanged -= OnPersonalizationSettingsPropertyChanged;

        if (_videosLibrary != null)
        {
            _videosLibrary.DefinitionChanged -= LibraryOnDefinitionChanged;
        }

        if (_musicLibrary != null)
        {
            _musicLibrary.DefinitionChanged -= LibraryOnDefinitionChanged;
        }

        if (SystemInformation.IsXbox && _portableStorageDeviceWatcher != null)
        {
            _portableStorageDeviceWatcher.Updated -= OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Removed -= OnPortableStorageDeviceChanged;
            _portableStorageDeviceWatcher.Stop();
        }
    }

    private void OnGeneralSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
    }

    private void OnPersonalizationSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PersonalizationSettingsContext.Theme))
        {
            OnPropertyChanged(nameof(Theme));
        }
    }

    private void OnPrivacySettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PrivacySettingsContext.ShowRecent))
        {
            if (!PrivacySettings.ShowRecent) ClearRecentContext();
        }
    }

    private void OnPlaybackSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaybackSettingsContext.MaxVolume))
        {
            OnPropertyChanged(nameof(VolumeBoost));
        }
    }

    private void OnPlayerSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerSettingsContext.AutoResize))
        {
            OnPropertyChanged(nameof(PlayerAutoResize));
        }
    }

    private async void OnLibrarySettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibrarySettingsContext.SearchRemovableStorage))
        {
            if (SystemInformation.IsXbox && RemovableStorageFolders.Count > 0)
            {
                await RefreshLibrariesAsync();
            }
        }
    }

    private void OnAdvancedSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdvancedSettingsContext.VideoUpscale))
        {
            OnPropertyChanged(nameof(VideoUpscaling));
            CheckForRelaunch();
        }
        else if (e.PropertyName == nameof(AdvancedSettingsContext.AdvancedMode))
        {
            CheckForRelaunch();
        }
        else if (e.PropertyName == nameof(AdvancedSettingsContext.GlobalArguments))
        {
            OnPropertyChanged(nameof(GlobalArguments));
            CheckForRelaunch();
        }
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

        bool upscalingChanged = _initialValues.VideoUpscaling != VideoUpscaling;
        bool languageChanged = _initialValues.Language != SelectedLanguage;
        bool argsChanged = _initialValues.GlobalArguments != AdvancedSettings.GlobalArguments;
        bool modeChanged = _initialValues.AdvancedMode != AdvancedSettings.AdvancedMode;
        bool hasArgs = AdvancedSettings.GlobalArguments.Length > 0;
        bool whenOn = modeChanged && AdvancedSettings.AdvancedMode && hasArgs;
        bool whenOff = modeChanged && !AdvancedSettings.AdvancedMode && ((!hasArgs && argsChanged) || hasArgs);
        bool whenOnAndChanged = AdvancedSettings.AdvancedMode && argsChanged;

        IsRelaunchRequired = upscalingChanged || languageChanged || whenOn || whenOff || whenOnAndChanged;
    }
}

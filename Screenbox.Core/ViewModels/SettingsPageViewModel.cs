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

public sealed partial class SettingsPageViewModel : ObservableRecipient,
    IRecipient<SettingChangedMessage<GeneralSettingsContext, ThemeOption>>,
    IRecipient<SettingChangedMessage<GeneralSettingsContext, bool>>,
    IRecipient<SettingChangedMessage<PlaybackSettingsContext, int>>,
    IRecipient<SettingChangedMessage<PlayerSettingsContext, PlayerAutoResizeOption>>,
    IRecipient<SettingChangedMessage<PlayerSettingsContext, VideoUpscaleOption>>,
    IRecipient<SettingChangedMessage<PlayerSettingsContext, bool>>,
    IRecipient<SettingChangedMessage<PlayerSettingsContext, string>>,
    IRecipient<SettingChangedMessage<LibrarySettingsContext, bool>>
{
    public PlayerSettingsContext PlayerSettings { get; }
    public LibrarySettingsContext LibrarySettings { get; }
    public PlaybackSettingsContext PlaybackSettings { get; }
    public GeneralSettingsContext GeneralSettings { get; }

    public int Theme
    {
        get => ((int)GeneralSettings.Theme + 2) % 3;
        set => GeneralSettings.Theme = (ThemeOption)((value + 1) % 3);
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
        get => (int)PlayerSettings.VideoUpscale;
        set => PlayerSettings.VideoUpscale = (VideoUpscaleOption)value;
    }

    public string GlobalArguments
    {
        get => PlayerSettings.GlobalArguments;
        set
        {
            string sanitized = VlcArgumentHelper.SanitizeArguments(value);
            PlayerSettings.GlobalArguments = sanitized;
            if (sanitized != value)
            {
                OnPropertyChanged();
            }
        }
    }

    public bool AdvancedMode
    {
        get => PlayerSettings.AdvancedMode;
        set => PlayerSettings.AdvancedMode = value;
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

        Messenger.Register<SettingChangedMessage<GeneralSettingsContext, ThemeOption>>(this);
        Messenger.Register<SettingChangedMessage<GeneralSettingsContext, bool>>(this);
        Messenger.Register<SettingChangedMessage<PlaybackSettingsContext, int>>(this);
        Messenger.Register<SettingChangedMessage<PlayerSettingsContext, PlayerAutoResizeOption>>(this);
        Messenger.Register<SettingChangedMessage<PlayerSettingsContext, VideoUpscaleOption>>(this);
        Messenger.Register<SettingChangedMessage<PlayerSettingsContext, bool>>(this);
        Messenger.Register<SettingChangedMessage<PlayerSettingsContext, string>>(this);
        Messenger.Register<SettingChangedMessage<LibrarySettingsContext, bool>>(this);

        // Setting initial values for relaunch check
        _initialValues ??= new InitialValues(PlayerSettings.GlobalArguments, PlayerSettings.AdvancedMode, VideoUpscaling, SelectedLanguage);
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
        Messenger.UnregisterAll(this);

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

    public void Receive(SettingChangedMessage<GeneralSettingsContext, ThemeOption> message)
    {
        if (message.PropertyName == nameof(GeneralSettingsContext.Theme))
        {
            OnPropertyChanged(nameof(Theme));
        }
    }

    public void Receive(SettingChangedMessage<GeneralSettingsContext, bool> message)
    {
        if (message.PropertyName == nameof(GeneralSettingsContext.ShowRecent))
        {
            if (!GeneralSettings.ShowRecent)
            {
                ClearRecentContext();
            }
        }
    }

    public void Receive(SettingChangedMessage<PlaybackSettingsContext, int> message)
    {
        if (message.PropertyName == nameof(PlaybackSettingsContext.MaxVolume))
        {
            OnPropertyChanged(nameof(VolumeBoost));
        }
    }

    public void Receive(SettingChangedMessage<PlayerSettingsContext, PlayerAutoResizeOption> message)
    {
        if (message.PropertyName == nameof(PlayerSettingsContext.AutoResize))
        {
            OnPropertyChanged(nameof(PlayerAutoResize));
        }
    }

    public void Receive(SettingChangedMessage<PlayerSettingsContext, VideoUpscaleOption> message)
    {
        if (message.PropertyName == nameof(PlayerSettingsContext.VideoUpscale))
        {
            OnPropertyChanged(nameof(VideoUpscaling));
            CheckForRelaunch();
        }
    }

    public void Receive(SettingChangedMessage<PlayerSettingsContext, bool> message)
    {
        if (message.PropertyName == nameof(PlayerSettingsContext.AdvancedMode))
        {
            CheckForRelaunch();
        }
    }

    public void Receive(SettingChangedMessage<PlayerSettingsContext, string> message)
    {
        if (message.PropertyName == nameof(PlayerSettingsContext.GlobalArguments))
        {
            OnPropertyChanged(nameof(GlobalArguments));
            CheckForRelaunch();
        }
    }

    public void Receive(SettingChangedMessage<LibrarySettingsContext, bool> message)
    {
        if (message.PropertyName == nameof(LibrarySettingsContext.SearchRemovableStorage))
        {
            if (SystemInformation.IsXbox && RemovableStorageFolders.Count > 0)
            {
                _dispatcherQueue.TryEnqueue(async () => await RefreshLibrariesAsync());
            }
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
        bool argsChanged = _initialValues.GlobalArguments != PlayerSettings.GlobalArguments;
        bool modeChanged = _initialValues.AdvancedMode != PlayerSettings.AdvancedMode;
        bool hasArgs = PlayerSettings.GlobalArguments.Length > 0;
        bool whenOn = modeChanged && PlayerSettings.AdvancedMode && hasArgs;
        bool whenOff = modeChanged && !PlayerSettings.AdvancedMode && ((!hasArgs && argsChanged) || hasArgs);
        bool whenOnAndChanged = PlayerSettings.AdvancedMode && argsChanged;

        IsRelaunchRequired = upscalingChanged || languageChanged || whenOn || whenOff || whenOnAndChanged;
    }
}

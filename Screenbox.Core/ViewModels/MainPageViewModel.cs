using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels;

public sealed partial class MainPageViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<PlayerVisibilityState>>,
    IRecipient<NavigationViewDisplayModeRequestMessage>,
    IRecipient<CriticalErrorMessage>
{
    private const int TriggerSeekMultiplier = 4;
    private const int VolumeAdjustmentStep = 2;

    [ObservableProperty] public partial bool PlayerVisible { get; set; }
    [ObservableProperty] public partial bool ShouldUseMargin { get; set; }
    [ObservableProperty] public partial bool IsPaneOpen { get; set; }
    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial string CriticalErrorMessage { get; set; }
    [ObservableProperty] public partial bool HasCriticalError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial NavigationViewDisplayMode NavigationViewDisplayMode { get; set; }

    private readonly ISettingsService _settingsService;
    private readonly ISearchService _searchService;
    private readonly INavigationService _navigationService;
    private readonly LibraryContext _libraryContext;
    private readonly ILibraryCoordinator _libraryCoordinator;
    private readonly PlaylistsContext _playlistsContext;
    private readonly IPlaylistService _playlistService;
    private readonly IPlaylistViewModelFactory _playlistFactory;
    private readonly ILogger<MainPageViewModel> _logger;

    /// <summary>
    /// Gets the collection of search suggestions for the current search query.
    /// </summary>
    /// <value>
    /// A collection of <see cref="SearchSuggestion"/> objects representing suggestions for the search box.
    /// </value>
    public ObservableCollection<SearchSuggestion> SearchSuggestions { get; }

    public MainPageViewModel(ISettingsService settingsService, ISearchService searchService, INavigationService navigationService,
        LibraryContext libraryContext, ILibraryCoordinator libraryCoordinator,
        PlaylistsContext playlistsContext, IPlaylistService playlistService, IPlaylistViewModelFactory playlistFactory,
        ILogger<MainPageViewModel> logger)
    {
        _settingsService = settingsService;
        _searchService = searchService;
        _navigationService = navigationService;
        _libraryContext = libraryContext;
        _libraryCoordinator = libraryCoordinator;
        _playlistsContext = playlistsContext;
        _playlistService = playlistService;
        _playlistFactory = playlistFactory;
        _logger = logger;
        SearchQuery = string.Empty;
        CriticalErrorMessage = string.Empty;
        SearchSuggestions = new ObservableCollection<SearchSuggestion>();

        Messenger.Register<PropertyChangedMessage<PlayerVisibilityState>>(this);
        Messenger.Register<NavigationViewDisplayModeRequestMessage>(this);
        Messenger.Register<CriticalErrorMessage>(this);
    }

    public void Receive(CriticalErrorMessage message)
    {
        HasCriticalError = true;
        CriticalErrorMessage = message.Message;
    }

    public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
    {
        PlayerVisible = message.NewValue == PlayerVisibilityState.Visible;
        ShouldUseMargin = message.NewValue != PlayerVisibilityState.Hidden;
    }

    public void Receive(NavigationViewDisplayModeRequestMessage message)
    {
        message.Reply(NavigationViewDisplayMode);
    }

    public bool TryGetPageTypeFromParameter(object? parameter, out Type pageType)
    {
        pageType = typeof(object);
        return parameter is NavigationMetadata metadata &&
               _navigationService.TryGetPageType(metadata.RootViewModelType, out pageType);
    }

    public void ProcessGamepadKeyDown(VirtualKey key)
    {
        // All Gamepad keys are in the range of [195, 218]
        if ((int)key < 195 || (int)key > 218) return;

        Playlist playlist = Messenger.Send(new QueueRequestMessage());
        if (playlist.IsEmpty) return;

        int rewindStep = _settingsService.PlayerRewindStep;
        int fastForwardStep = _settingsService.PlayerFastForwardStep;

        switch (key)
        {
            case VirtualKey.GamepadRightThumbstickLeft:
            case VirtualKey.GamepadLeftShoulder:
                Messenger.SendSeekWithStatus(TimeSpan.FromSeconds(-rewindStep));
                break;
            case VirtualKey.GamepadRightThumbstickRight:
            case VirtualKey.GamepadRightShoulder:
                Messenger.SendSeekWithStatus(TimeSpan.FromSeconds(fastForwardStep));
                break;
            case VirtualKey.GamepadLeftTrigger when PlayerVisible:
                Messenger.SendSeekWithStatus(TimeSpan.FromSeconds(-rewindStep * TriggerSeekMultiplier));
                break;
            case VirtualKey.GamepadRightTrigger when PlayerVisible:
                Messenger.SendSeekWithStatus(TimeSpan.FromSeconds(fastForwardStep * TriggerSeekMultiplier));
                break;
            case VirtualKey.GamepadRightThumbstickUp:
                int volumeUp = Messenger.Send(new ChangeVolumeRequestMessage(VolumeAdjustmentStep, true));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Volume, value: volumeUp).ShowBadge().ShowMessage());
                break;
            case VirtualKey.GamepadRightThumbstickDown:
                int volumeDown = Messenger.Send(new ChangeVolumeRequestMessage(-VolumeAdjustmentStep, true));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Volume, value: volumeDown).ShowBadge().ShowMessage());
                break;
            case VirtualKey.GamepadX:
                Messenger.Send(new TogglePlayPauseMessage(true));
                break;
            case VirtualKey.GamepadView when PlayerVisible || NavigationViewDisplayMode == NavigationViewDisplayMode.Expanded:
                Messenger.Send(new TogglePlayerVisibilityMessage());
                break;
            default:
                return;
        }
    }

    public void OnDrop(DataPackageView data)
    {
        Messenger.Send(new DragDropMessage(data));
    }

    /// <summary>
    /// Updates the <see cref="SearchSuggestions"/> collection based on the specified
    /// search query text.
    /// </summary>
    /// <param name="text">A search query string used to filter suggestions.</param>
    public void UpdateSearchSuggestions(string text)
    {
        _searchService.PopulateSuggestions(SearchSuggestions, text);
    }

    public void SubmitSearch(string queryText)
    {
        string searchQuery = queryText.Trim();
        if (searchQuery.Length > 0)
        {
            SearchResult result = _searchService.SearchLocalLibrary(_libraryContext, searchQuery);
            _navigationService.Navigate(typeof(SearchResultPageViewModel), result);
        }
    }

    public void SelectSuggestion(SearchSuggestion chosenSuggestion)
    {
        if (chosenSuggestion.Data is null) return;

        switch (chosenSuggestion.Data)
        {
            case MediaViewModel media:
                Messenger.Send(new PlayMediaMessage(media));
                break;
            case AlbumViewModel album:
                _navigationService.Navigate(typeof(AlbumDetailsPageViewModel), album);
                break;
            case ArtistViewModel artist:
                _navigationService.Navigate(typeof(ArtistDetailsPageViewModel), artist);
                break;
        }
    }

    public async Task FetchLibraries()
    {
        try
        {
            await _libraryCoordinator.EnsureWatchingAsync();
        }
        catch (Exception)
        {
            // pass
        }

        try
        {
            await Task.WhenAll(
                FetchMusicLibraryAsync(),
                FetchVideosLibraryAsync(),
                FetchPlaylistsAsync());

            // Fetch playlists again to ensure items are updated after libraries are fetched
            // This is necessary because playlist items may reference media that is not yet loaded in the library context
            // Fetching library may take too long so we can't wait for it to finish before fetching playlists
            await FetchPlaylistsAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch startup libraries.");
        }
    }

    private async Task FetchMusicLibraryAsync()
    {
        try
        {
            await _libraryCoordinator.FetchMusicAsync();
        }
        catch (UnauthorizedAccessException)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MusicLibraryAccessDenied));
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.None, message: e.Message));
            _logger.LogError(e, "Failed to fetch the music library.");
        }
    }

    private async Task FetchVideosLibraryAsync()
    {
        try
        {
            await _libraryCoordinator.FetchVideosAsync();
        }
        catch (UnauthorizedAccessException)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.VideosLibraryAccessDenied));
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.None, message: e.Message));
            _logger.LogError(e, "Failed to fetch the videos library.");
        }
    }

    /// <summary>
    /// Fetches playlists from storage and populates the PlaylistsContext.
    /// </summary>
    private async Task FetchPlaylistsAsync()
    {
        try
        {
            var loaded = await _playlistService.ListPlaylistsAsync();
            _playlistsContext.Playlists.Clear();
            foreach (var p in loaded)
            {
                var playlist = _playlistFactory.Create();
                try
                {
                    playlist.Load(p);
                    _playlistsContext.Playlists.Add(playlist);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to load playlist '{PlaylistId}'.", p.Id);
                }
            }
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.None, message: e.Message));
            _logger.LogError(e, "Failed to fetch playlists.");
        }
    }
}

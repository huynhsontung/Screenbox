#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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

    private const int MaxSuggestionsPerCategory = 6;
    private const int MaxTotalSuggestions = 10;
    private const double IndexWeightFactor = 0.1;

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

    /// <summary>
    /// Gets the collection of search suggestions for the current search query.
    /// </summary>
    /// <value>
    /// A collection of <see cref="SearchSuggestion"/> objects representing suggestions for the search box.
    /// </value>
    public ObservableCollection<SearchSuggestion> SearchSuggestions { get; }

    public MainPageViewModel(ISettingsService settingsService, ISearchService searchService, INavigationService navigationService,
        LibraryContext libraryContext, ILibraryCoordinator libraryCoordinator,
        PlaylistsContext playlistsContext, IPlaylistService playlistService, IPlaylistViewModelFactory playlistFactory)
    {
        _settingsService = settingsService;
        _searchService = searchService;
        _navigationService = navigationService;
        _libraryContext = libraryContext;
        _libraryCoordinator = libraryCoordinator;
        _playlistsContext = playlistsContext;
        _playlistService = playlistService;
        _playlistFactory = playlistFactory;
        SearchQuery = string.Empty;
        CriticalErrorMessage = string.Empty;
        SearchSuggestions = new ObservableCollection<SearchSuggestion>();

        IsActive = true;
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
                Messenger.Send(new UpdateVolumeStatusMessage(volumeUp));
                break;
            case VirtualKey.GamepadRightThumbstickDown:
                int volumeDown = Messenger.Send(new ChangeVolumeRequestMessage(-VolumeAdjustmentStep, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volumeDown));
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
        if (string.IsNullOrWhiteSpace(text))
        {
            SearchSuggestions.Clear();
            return;
        }

        string queryText = text.Trim();
        var result = _searchService.SearchLocalLibrary(_libraryContext, queryText);
        var newSuggestions = GetSuggestItems(result, queryText).ToList();

        if (newSuggestions.Count == 0)
        {
            newSuggestions.Add(new SearchSuggestion(SearchSuggestionType.None, queryText));
        }

        for (int i = 0; i < newSuggestions.Count; i++)
        {
            if (i < SearchSuggestions.Count)
            {
                var newItem = newSuggestions[i];
                var oldItem = SearchSuggestions[i];
                if (!Equals(oldItem, newItem))
                {
                    SearchSuggestions[i] = newItem;
                }
            }
            else
            {
                SearchSuggestions.Add(newSuggestions[i]);
            }
        }

        for (int i = SearchSuggestions.Count - 1; i >= newSuggestions.Count; i--)
        {
            SearchSuggestions.RemoveAt(i);
        }
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

    private IReadOnlyList<SearchSuggestion> GetSuggestItems(SearchResult result, string searchQuery)
    {
        if (!result.HasItems) return Array.Empty<SearchSuggestion>();

        IEnumerable<SearchSuggestion> songs = result.Songs
            .Take(MaxSuggestionsPerCategory)
            .Select(s => new SearchSuggestion(SearchSuggestionType.Song, s.Name, s));
        IEnumerable<SearchSuggestion> videos = result.Videos
            .Take(MaxSuggestionsPerCategory)
            .Select(v => new SearchSuggestion(SearchSuggestionType.Video, v.Name, v));
        IEnumerable<SearchSuggestion> artists = result.Artists
            .Take(MaxSuggestionsPerCategory)
            .Select(a => new SearchSuggestion(SearchSuggestionType.Artist, a.Name, a));
        IEnumerable<SearchSuggestion> albums = result.Albums
            .Take(MaxSuggestionsPerCategory)
            .Select(a => new SearchSuggestion(SearchSuggestionType.Album, a.Name, a));
        IEnumerable<(double, SearchSuggestion)> searchResults = songs
            .Concat(videos).Concat(artists).Concat(albums)
            .Select(item => (GetRanking(item.Text, searchQuery), item))
            .OrderBy(t => t.Item1)
            .Take(MaxTotalSuggestions);

        return searchResults.Select(t => t.Item2).ToArray();
    }

    private static double GetRanking(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
            return -1;

        int index = text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        if (query.Contains(' ') || index < 0)
        {
            return index;
        }

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double wordRank = words
            .Select(s => s.IndexOf(query, StringComparison.CurrentCultureIgnoreCase))
            .Where(i => i >= 0)
            .Average();
        return (index * IndexWeightFactor) + wordRank;
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
            LogService.Log(e);
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
            Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Music));
        }
        catch (Exception e)
        {
            Messenger.Send(new ErrorMessage(null, e.Message));
            LogService.Log(e);
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
            Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Videos));
        }
        catch (Exception e)
        {
            Messenger.Send(new ErrorMessage(null, e.Message));
            LogService.Log(e);
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
                    LogService.Log(e);
                }
            }
        }
        catch (Exception e)
        {
            Messenger.Send(new ErrorMessage(null, e.Message));
            LogService.Log(e);
        }
    }
}

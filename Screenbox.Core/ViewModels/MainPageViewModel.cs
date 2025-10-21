﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Core.ViewModels;

public sealed partial class MainPageViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<PlayerVisibilityState>>,
    IRecipient<NavigationViewDisplayModeRequestMessage>,
    IRecipient<CriticalErrorMessage>
{
    private const int MaxSuggestionsPerCategory = 6;
    private const int MaxTotalSuggestions = 10;
    private const string AudioGlyph = "\uE8D6";
    private const string MoviesGlyph = "\uE8B2";
    private const string ContactGlyph = "\uE77B";
    private const string MusicAlbumGlyph = "\uE93C";
    private const double IndexWeightFactor = 0.1;

    [ObservableProperty] private bool _playerVisible;
    [ObservableProperty] private bool _shouldUseMargin;
    [ObservableProperty] private bool _isPaneOpen;
    [ObservableProperty] private string _searchQuery;
    [ObservableProperty] private string _criticalErrorMessage;
    [ObservableProperty] private bool _hasCriticalError;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private NavigationViewDisplayMode _navigationViewDisplayMode;

    private readonly ISearchService _searchService;
    private readonly INavigationService _navigationService;
    private readonly ILibraryService _libraryService;

    public ObservableCollection<SearchSuggestionItem> SearchSuggestions { get; } = new();

    public MainPageViewModel(ISearchService searchService, INavigationService navigationService,
        ILibraryService libraryService)
    {
        _searchService = searchService;
        _navigationService = navigationService;
        _libraryService = libraryService;
        _searchQuery = string.Empty;
        _criticalErrorMessage = string.Empty;
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

    public void ProcessGamepadKeyDown(KeyRoutedEventArgs args)
    {
        // All Gamepad keys are in the range of [195, 218]
        if ((int)args.Key < 195 || (int)args.Key > 218) return;
        Playlist playlist = Messenger.Send(new PlaylistRequestMessage());
        if (playlist.CurrentItem == null) return;
        int volumeChange = 0;
        switch (args.Key)
        {
            case VirtualKey.GamepadRightThumbstickLeft:
            case VirtualKey.GamepadLeftShoulder:
                Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(-5000));
                break;
            case VirtualKey.GamepadRightThumbstickRight:
            case VirtualKey.GamepadRightShoulder:
                Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(5000));
                break;
            case VirtualKey.GamepadLeftTrigger when PlayerVisible:
                Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(-30_000));
                break;
            case VirtualKey.GamepadRightTrigger when PlayerVisible:
                Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(30_000));
                break;
            case VirtualKey.GamepadRightThumbstickUp:
                volumeChange = 2;
                break;
            case VirtualKey.GamepadRightThumbstickDown:
                volumeChange = -2;
                break;
            case VirtualKey.GamepadX:
                Messenger.Send(new TogglePlayPauseMessage(true));
                break;
            case VirtualKey.GamepadView when !PlayerVisible:
                Messenger.Send(new TogglePlayerVisibilityMessage());
                break;
            default:
                return;
        }

        if (volumeChange != 0)
        {
            int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
            Messenger.Send(new UpdateVolumeStatusMessage(volume));
        }

        args.Handled = true;
    }

    public void OnDrop(DataPackageView data)
    {
        Messenger.Send(new DragDropMessage(data));
    }

    public void UpdateSearchSuggestions(string queryText)
    {
        string searchQuery = queryText.Trim();
        if (searchQuery.Length > 0)
        {
            SearchSuggestions.Clear();

            var result = _searchService.SearchLocalLibrary(searchQuery);
            var suggestions = GetSuggestItems(result, searchQuery);

            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
        }
    }

    public void SubmitSearch(string queryText)
    {
        string searchQuery = queryText.Trim();
        if (searchQuery.Length > 0)
        {
            SearchResult result = _searchService.SearchLocalLibrary(searchQuery);
            _navigationService.Navigate(typeof(SearchResultPageViewModel), result);
        }
    }

    public void SelectSuggestion(SearchSuggestionItem? chosenSuggestion)
    {
        if (chosenSuggestion?.Data == null) return;

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

    private IReadOnlyList<SearchSuggestionItem> GetSuggestItems(SearchResult result, string searchQuery)
    {
        if (!result.HasItems) return Array.Empty<SearchSuggestionItem>();

        IEnumerable<SearchSuggestionItem> songs = result.Songs
            .Take(MaxSuggestionsPerCategory).Select(s => new SearchSuggestionItem(s.Name, s, AudioGlyph));
        IEnumerable<SearchSuggestionItem> videos = result.Videos
            .Take(MaxSuggestionsPerCategory).Select(v => new SearchSuggestionItem(v.Name, v, MoviesGlyph));
        IEnumerable<SearchSuggestionItem> artists = result.Artists
            .Take(MaxSuggestionsPerCategory).Select(a => new SearchSuggestionItem(a.Name, a, ContactGlyph));
        IEnumerable<SearchSuggestionItem> albums = result.Albums
            .Take(MaxSuggestionsPerCategory).Select(a => new SearchSuggestionItem(a.Name, a, MusicAlbumGlyph));
        IEnumerable<(double, SearchSuggestionItem)> searchResults = songs.Concat(videos).Concat(artists).Concat(albums)
            .Select(item => (GetRanking(item.Name, searchQuery), item))
            .OrderBy(t => t.Item1)
            .Take(MaxTotalSuggestions);

        return searchResults.Select(t => t.Item2).ToArray();
    }

    private static double GetRanking(string text, string query)
    {
        int index = text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        if (query.Contains(' '))
        {
            return index;
        }

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double wordRank = words
            .Select(s => s.IndexOf(query, StringComparison.CurrentCultureIgnoreCase))
            .Where(i => i >= 0)
            .Average();
        return index * IndexWeightFactor + wordRank;
    }

    public Task FetchLibraries()
    {
        List<Task> tasks = new() { FetchMusicLibraryAsync(), FetchVideosLibraryAsync() };
        return Task.WhenAll(tasks);
    }

    private async Task FetchMusicLibraryAsync()
    {
        try
        {
            await _libraryService.FetchMusicAsync();
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
            await _libraryService.FetchVideosAsync();
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
}

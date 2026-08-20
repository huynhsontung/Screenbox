using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels;

public sealed partial class AlbumsPageViewModel : BaseMusicContentViewModel,
    IRecipient<PropertyChangedMessage<MusicLibrary>>
{
    public ObservableCollection<ObservableAlbumGroup> GroupedAlbums { get; } = [];

    [ObservableProperty]
    public partial AlbumSortOrder SortBy { get; set; } = AlbumSortOrder.Title;

    [ObservableProperty]
    public partial string? SelectedGenre { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> Genres { get; set; } = Array.Empty<string>();

    [ObservableProperty]
    public partial AlbumViewModel? ContextAlbum { get; set; }

    private readonly LibraryContext _libraryContext;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _refreshTimer;

    public AlbumsPageViewModel(LibraryContext libraryContext, ISettingsService settingsService)
    {
        _libraryContext = libraryContext;
        _settingsService = settingsService;
        SortBy = _settingsService.PersistentAlbumsSortOrder;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _refreshTimer = _dispatcherQueue.CreateTimer();

        Messenger.Register<PropertyChangedMessage<MusicLibrary>>(this);
    }

    public void Receive(PropertyChangedMessage<MusicLibrary> message)
    {
        _dispatcherQueue.TryEnqueue(FetchAlbums);
    }

    public void OnNavigatedFrom()
    {
        _refreshTimer.Stop();
    }

    public void FetchAlbums()
    {
        // No need to run fetch async. HomePageViewModel should already called the method.
        IsLoading = _libraryContext.IsLoadingMusic;
        Genres = _libraryContext.Music.Genres;
        Songs = GetFilteredSongs().ToList();

        var groups = GetCurrentGrouping(_libraryContext, SortBy);
        if (Songs.Count < 5000)
        {
            // Only sync when the number of items is low enough
            // Sync on too many items can cause UI hang
            GroupedAlbums.SyncObservableGroups(groups, (key, items)
                => items as ObservableAlbumGroup ?? new ObservableAlbumGroup(key, items));
        }
        else
        {
            GroupedAlbums.Clear();
            foreach (IGrouping<string, AlbumViewModel> group in groups)
            {
                GroupedAlbums.Add(new ObservableAlbumGroup(group));
            }
        }

        // Progressively update when it's still loading
        if (_libraryContext.IsLoadingMusic)
        {
            _refreshTimer.Debounce(FetchAlbums, TimeSpan.FromSeconds(5));
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    private IEnumerable<MediaViewModel> GetFilteredSongs()
    {
        IReadOnlyList<MediaViewModel> allSongs = _libraryContext.Music.Songs;
        return SelectedGenre switch
        {
            null => allSongs,
            "" => allSongs.Where(s => string.IsNullOrWhiteSpace(s.MediaInfo.MusicProperties.Genre)),
            _ => allSongs.Where(s => string.Equals(s.MediaInfo.MusicProperties.Genre.Trim(), SelectedGenre, StringComparison.CurrentCultureIgnoreCase))
        };
    }

    private IEnumerable<AlbumViewModel> GetFilteredAlbums(LibraryContext context)
    {
        IEnumerable<AlbumViewModel> allAlbums = context.Music.Albums.Values;
        return SelectedGenre switch
        {
            null => allAlbums,
            "" => allAlbums.Where(a => a.RelatedSongs.Any(s => string.IsNullOrWhiteSpace(s.MediaInfo.MusicProperties.Genre))),
            _ => allAlbums.Where(a => a.RelatedSongs.Any(s => string.Equals(s.MediaInfo.MusicProperties.Genre.Trim(), SelectedGenre, StringComparison.CurrentCultureIgnoreCase)))
        };
    }

    private List<IGrouping<string, AlbumViewModel>> GetDefaultGrouping(LibraryContext context, IEnumerable<AlbumViewModel> albums)
    {
        var groups = albums
            .OrderBy(a => a.Name, StringComparer.CurrentCulture)
            .GroupBy(album => album == context.Music.UnknownAlbum
                ? MediaGroupingHelpers.OtherGroupSymbol
                : MediaGroupingHelpers.GetCharacterGroupLabel(album.Name))
            .ToList();

        var sortedGroup = new List<IGrouping<string, AlbumViewModel>>();
        foreach (string groupHeader in MediaGroupingHelpers.CharacterGroupLabels)
        {
            if (groups.Find(g => g.Key == groupHeader) is { } group)
            {
                sortedGroup.Add(group);
            }
            else
            {
                sortedGroup.Add(new ListGrouping<string, AlbumViewModel>(groupHeader));
            }
        }

        return sortedGroup;
    }

    private List<IGrouping<string, AlbumViewModel>> GetArtistGrouping(LibraryContext context, IEnumerable<AlbumViewModel> albums)
    {
        var groups = albums.GroupBy(a => a.ArtistName)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture)
            .ToList();

        var index = groups.FindIndex(g => g.Key == context.Music.UnknownArtist.Name);
        if (index >= 0)
        {
            var firstGroup = groups[index];
            groups.RemoveAt(index);
            groups.Insert(0, firstGroup);
        }

        return groups;
    }

    private List<IGrouping<string, AlbumViewModel>> GetYearGrouping(IEnumerable<AlbumViewModel> albums)
    {
        var groups = albums.GroupBy(a =>
                a.Year > 0
                    ? a.Year.ToString() ?? MediaGroupingHelpers.OtherGroupSymbol
                    : MediaGroupingHelpers.OtherGroupSymbol)
            .OrderByDescending(g => g.Key == MediaGroupingHelpers.OtherGroupSymbol ? 0 : uint.Parse(g.Key))
            .ToList();
        return groups;
    }

    private List<IGrouping<string, AlbumViewModel>> GetDateAddedGrouping(IEnumerable<AlbumViewModel> albums)
    {
        var groups = albums.GroupBy(a => a.DateAdded.Date)
            .OrderByDescending(g => g.Key)
            .Select(g =>
                new ListGrouping<string, AlbumViewModel>(
                    g.Key == default ? MediaGroupingHelpers.OtherGroupSymbol : g.Key.ToString("d", CultureInfo.CurrentCulture), g))
            .OfType<IGrouping<string, AlbumViewModel>>()
            .ToList();
        return groups;
    }

    private List<IGrouping<string, AlbumViewModel>> GetCurrentGrouping(LibraryContext context, AlbumSortOrder sortBy)
    {
        var albums = GetFilteredAlbums(context);
        return sortBy switch
        {
            AlbumSortOrder.Artist => GetArtistGrouping(context, albums),
            AlbumSortOrder.Year => GetYearGrouping(albums),
            AlbumSortOrder.DateAdded => GetDateAddedGrouping(albums),
            _ => GetDefaultGrouping(context, albums)
        };
    }

    public async void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase != 0) return;
        if (args.Item is AlbumViewModel album)
        {
            await album.LoadAlbumArtAsync();
        }
    }

    partial void OnSortByChanged(AlbumSortOrder value)
    {
        _settingsService.PersistentAlbumsSortOrder = value;
        UpdateGrouping();
    }

    partial void OnSelectedGenreChanged(string? value)
    {
        Songs = GetFilteredSongs().ToList();
        UpdateGrouping();
    }

    private void UpdateGrouping()
    {
        var groups = GetCurrentGrouping(_libraryContext, SortBy);
        GroupedAlbums.Clear();
        foreach (IGrouping<string, AlbumViewModel> group in groups)
        {
            GroupedAlbums.Add(new ObservableAlbumGroup(group));
        }
    }

    [RelayCommand]
    private void SetSortBy(AlbumSortOrder tag)
    {
        SortBy = tag;
    }

    [RelayCommand]
    private void SetGenre(string? genre)
    {
        SelectedGenre = genre;
    }
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels;

public sealed partial class AlbumsPageViewModel : BaseMusicContentViewModel,
    IRecipient<LibraryContentChangedMessage>
{
    public ObservableGroupedCollection<string, AlbumViewModel> GroupedAlbums { get; }

    [ObservableProperty]
    private string _sortBy = string.Empty;

    private readonly LibraryContext _libraryContext;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _refreshTimer;

    public AlbumsPageViewModel(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _refreshTimer = _dispatcherQueue.CreateTimer();
        GroupedAlbums = new ObservableGroupedCollection<string, AlbumViewModel>();

        IsActive = true;
    }

    public void Receive(LibraryContentChangedMessage message)
    {
        if (message.LibraryId != KnownLibraryId.Music) return;
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
        Songs = _libraryContext.Songs;

        var groups = GetCurrentGrouping(_libraryContext, SortBy);
        if (Songs.Count < 5000)
        {
            // Only sync when the number of items is low enough
            // Sync on too many items can cause UI hang
            GroupedAlbums.SyncObservableGroups(groups);
        }
        else
        {
            GroupedAlbums.Clear();
            foreach (IGrouping<string, AlbumViewModel> group in groups)
            {
                GroupedAlbums.AddGroup(group);
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

    private List<IGrouping<string, AlbumViewModel>> GetDefaultGrouping(LibraryContext context)
    {
        var groups = context.Albums.Values
            .OrderBy(a => a.Name, StringComparer.CurrentCulture)
            .GroupBy(album => album == context.UnknownAlbum
                ? MediaGroupingHelpers.OtherGroupSymbol
                : MediaGroupingHelpers.GetFirstLetterGroup(album.Name))
            .ToList();

        var sortedGroup = new List<IGrouping<string, AlbumViewModel>>();
        foreach (char header in MediaGroupingHelpers.GroupHeaders)
        {
            string groupHeader = header.ToString();
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

    private List<IGrouping<string, AlbumViewModel>> GetArtistGrouping(LibraryContext context)
    {
        var groups = context.Albums.Values.GroupBy(a => a.ArtistName)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture)
            .ToList();

        var index = groups.FindIndex(g => g.Key == context.UnknownArtist.Name);
        if (index >= 0)
        {
            var firstGroup = groups[index];
            groups.RemoveAt(index);
            groups.Insert(0, firstGroup);
        }

        return groups;
    }

    private List<IGrouping<string, AlbumViewModel>> GetYearGrouping(LibraryContext context)
    {
        var groups = context.Albums.Values.GroupBy(a =>
                a.Year > 0
                    ? a.Year.ToString()
                    : MediaGroupingHelpers.OtherGroupSymbol)
            .OrderByDescending(g => g.Key == MediaGroupingHelpers.OtherGroupSymbol ? 0 : uint.Parse(g.Key))
            .ToList();
        return groups;
    }

    private List<IGrouping<string, AlbumViewModel>> GetDateAddedGrouping(LibraryContext context)
    {
        var groups = context.Albums.Values.GroupBy(a => a.DateAdded.Date)
            .OrderByDescending(g => g.Key)
            .Select(g =>
                new ListGrouping<string, AlbumViewModel>(
                    g.Key == default ? MediaGroupingHelpers.OtherGroupSymbol : g.Key.ToString("d", CultureInfo.CurrentCulture), g))
            .OfType<IGrouping<string, AlbumViewModel>>()
            .ToList();
        return groups;
    }

    private List<IGrouping<string, AlbumViewModel>> GetCurrentGrouping(LibraryContext context, string sortBy)
    {
        return sortBy switch
        {
            "artist" => GetArtistGrouping(context),
            "year" => GetYearGrouping(context),
            "dateAdded" => GetDateAddedGrouping(context),
            _ => GetDefaultGrouping(context)
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

    partial void OnSortByChanged(string value)
    {
        var groups = GetCurrentGrouping(_libraryContext, value);
        GroupedAlbums.Clear();
        foreach (IGrouping<string, AlbumViewModel> group in groups)
        {
            GroupedAlbums.AddGroup(group);
        }
    }

    [RelayCommand]
    private void SetSortBy(string tag)
    {
        SortBy = tag;
    }
}

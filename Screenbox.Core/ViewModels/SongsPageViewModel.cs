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

namespace Screenbox.Core.ViewModels;

public sealed partial class SongsPageViewModel : BaseMusicContentViewModel,
    IRecipient<LibraryContentChangedMessage>
{
    public ObservableGroupedCollection<string, MediaViewModel> GroupedSongs { get; }

    [ObservableProperty]
    private string _sortBy = string.Empty;

    private readonly LibraryContext _libraryContext;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _refreshTimer;

    public SongsPageViewModel(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _refreshTimer = _dispatcherQueue.CreateTimer();
        GroupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();

        IsActive = true;
    }

    public void Receive(LibraryContentChangedMessage message)
    {
        if (message.LibraryId != KnownLibraryId.Music) return;
        _dispatcherQueue.TryEnqueue(FetchSongs);
    }

    public void OnNavigatedFrom()
    {
        _refreshTimer.Stop();
    }

    public void FetchSongs()
    {
        // No need to run fetch async. HomePageViewModel should already called the method.
        IsLoading = _libraryContext.IsLoadingMusic;
        Songs = _libraryContext.Songs;

        // Populate song groups with fetched result
        var groups = GetCurrentGrouping(_libraryContext, SortBy);
        if (Songs.Count < 5000)
        {
            // Only sync when the number of items is low enough
            // Sync on too many items can cause UI hang
            GroupedSongs.SyncObservableGroups(groups);
        }
        else
        {
            GroupedSongs.Clear();
            foreach (IGrouping<string, MediaViewModel> group in groups)
            {
                GroupedSongs.AddGroup(group);
            }
        }

        // Progressively update when it's still loading
        if (_libraryContext.IsLoadingMusic)
        {
            _refreshTimer.Debounce(FetchSongs, TimeSpan.FromSeconds(5));
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    private List<IGrouping<string, MediaViewModel>> GetAlbumGrouping(LibraryContext context)
    {
        var groups = Songs.GroupBy(m => m.Album?.Name ?? context.UnknownAlbum.Name)
            .OrderBy(g => g.Key)
            .ToList();

        var index = groups.FindIndex(g => g.Key == context.UnknownAlbum.Name);
        if (index >= 0)
        {
            var firstGroup = groups[index];
            groups.RemoveAt(index);
            groups.Insert(0, firstGroup);
        }

        return groups;
    }

    private List<IGrouping<string, MediaViewModel>> GetArtistGrouping(LibraryContext context)
    {
        var groups = Songs.GroupBy(m => m.MainArtist?.Name ?? context.UnknownArtist.Name)
            .OrderBy(g => g.Key)
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

    private List<IGrouping<string, MediaViewModel>> GetYearGrouping()
    {
        var groups = Songs.GroupBy(m =>
                m.MediaInfo.MusicProperties.Year > 0
                    ? m.MediaInfo.MusicProperties.Year.ToString()
                    : MediaGroupingHelpers.OtherGroupSymbol)
            .OrderByDescending(g => g.Key == MediaGroupingHelpers.OtherGroupSymbol ? 0 : uint.Parse(g.Key))
            .ToList();
        return groups;
    }

    private List<IGrouping<string, MediaViewModel>> GetDateAddedGrouping()
    {
        var groups = Songs.GroupBy(m => m.DateAdded.Date)
            .OrderByDescending(g => g.Key)
            .Select(g =>
                new ListGrouping<string, MediaViewModel>(
                    g.Key == default ? MediaGroupingHelpers.OtherGroupSymbol : g.Key.ToString("d", CultureInfo.CurrentCulture), g))
            .OfType<IGrouping<string, MediaViewModel>>()
            .ToList();
        return groups;
    }

    private List<IGrouping<string, MediaViewModel>> GetDefaultGrouping()
    {
        var groups = Songs
            .GroupBy(m => MediaGroupingHelpers.GetFirstLetterGroup(m.Name))
            .ToList();

        var sortedGroup = new List<IGrouping<string, MediaViewModel>>();
        foreach (char header in MediaGroupingHelpers.GroupHeaders)
        {
            string groupHeader = header.ToString();
            if (groups.Find(g => g.Key == groupHeader) is { } group)
            {
                sortedGroup.Add(group);
            }
            else
            {
                sortedGroup.Add(new ListGrouping<string, MediaViewModel>(groupHeader));
            }
        }

        return sortedGroup;
    }

    private List<IGrouping<string, MediaViewModel>> GetCurrentGrouping(LibraryContext context, string sortBy)
    {
        return sortBy switch
        {
            "album" => GetAlbumGrouping(context),
            "artist" => GetArtistGrouping(context),
            "year" => GetYearGrouping(),
            "dateAdded" => GetDateAddedGrouping(),
            _ => GetDefaultGrouping()
        };
    }

    partial void OnSortByChanged(string value)
    {
        var groups = GetCurrentGrouping(_libraryContext, value);
        GroupedSongs.Clear();
        foreach (IGrouping<string, MediaViewModel> group in groups)
        {
            GroupedSongs.AddGroup(group);
        }
    }

    [RelayCommand]
    private void SetSortBy(string tag)
    {
        SortBy = tag;
    }

    [RelayCommand]
    private void Play(MediaViewModel media)
    {
        if (Songs.Count == 0) return;
        Messenger.SendQueueAndPlay(media, Songs);
    }
}

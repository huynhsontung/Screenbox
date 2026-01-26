using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed class ArtistsPageViewModel : BaseMusicContentViewModel,
    IRecipient<PropertyChangedMessage<MusicLibrary>>
{
    public ObservableGroupedCollection<string, ArtistViewModel> GroupedArtists { get; }

    private readonly LibraryContext _libraryContext;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _refreshTimer;

    public ArtistsPageViewModel(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _refreshTimer = _dispatcherQueue.CreateTimer();
        GroupedArtists = new ObservableGroupedCollection<string, ArtistViewModel>();
        PopulateGroups();

        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<MusicLibrary> message)
    {
        if (message.Sender is not LibraryContext) return;
        _dispatcherQueue.TryEnqueue(FetchArtists);
    }

    public void OnNavigatedFrom()
    {
        _refreshTimer.Stop();
    }

    public void FetchArtists()
    {
        // No need to run fetch async. HomePageViewModel should already called the method.
        Songs = _libraryContext.MusicLibrary.Songs;

        var groupings = GetDefaultGrouping(_libraryContext.MusicLibrary);
        GroupedArtists.SyncObservableGroups(groupings);

        // Progressively update when it's still loading
        if (_libraryContext.IsLoadingMusic)
        {
            _refreshTimer.Debounce(FetchArtists, TimeSpan.FromSeconds(5));
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    private List<IGrouping<string, ArtistViewModel>> GetDefaultGrouping(MusicLibrary library)
    {
        var groups = library.Artists.Values
            .OrderBy(a => a.Name, StringComparer.CurrentCulture)
            .GroupBy(artist => artist == library.UnknownArtist
                ? MediaGroupingHelpers.OtherGroupSymbol
                : MediaGroupingHelpers.GetFirstLetterGroup(artist.Name))
            .ToList();

        var sortedGroup = new List<IGrouping<string, ArtistViewModel>>();
        foreach (char header in MediaGroupingHelpers.GroupHeaders)
        {
            string groupHeader = header.ToString();
            if (groups.Find(g => g.Key == groupHeader) is { } group)
            {
                sortedGroup.Add(group);
            }
            else
            {
                sortedGroup.Add(new ListGrouping<string, ArtistViewModel>(groupHeader));
            }
        }

        return sortedGroup;
    }

    private void PopulateGroups()
    {
        foreach (string key in MediaGroupingHelpers.GroupHeaders.Select(letter => letter.ToString()))
        {
            GroupedArtists.AddGroup(key);
        }
    }
}

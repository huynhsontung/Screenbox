using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.WinUI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed class ArtistsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, ArtistViewModel> GroupedArtists { get; }

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        public ArtistsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            GroupedArtists = new ObservableGroupedCollection<string, ArtistViewModel>();
            PopulateGroups();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public void FetchArtists()
        {
            // No need to run fetch async. HomePageViewModel should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            Songs = musicLibrary.Songs;

            var groupings = GetDefaultGrouping(musicLibrary);
            GroupedArtists.SyncObservableGroups(groupings);

            // Progressively update when it's still loading
            if (_libraryService.IsLoadingMusic)
            {
                _refreshTimer.Debounce(FetchArtists, TimeSpan.FromSeconds(5));
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        private List<IGrouping<string, ArtistViewModel>> GetDefaultGrouping(MusicLibraryFetchResult fetchResult)
        {
            var groups = fetchResult.Artists
                .OrderBy(a => a.Name, StringComparer.CurrentCulture)
                .GroupBy(artist => artist == fetchResult.UnknownArtist
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

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(FetchArtists);
        }
    }
}

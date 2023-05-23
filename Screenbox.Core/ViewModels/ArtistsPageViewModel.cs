using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Linq;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed class ArtistsPageViewModel : ObservableRecipient
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
            // No need to run fetch async. Music page should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();

            GroupedArtists.ClearItems();
            foreach (ArtistViewModel artist in musicLibrary.Artists.OrderBy(a => a.Name, StringComparer.CurrentCulture))
            {
                string key = artist == musicLibrary.UnknownArtist
                    ? "\u2026"
                    : MediaGroupingHelpers.GetFirstLetterGroup(artist.Name);
                GroupedArtists.AddItem(key, artist);
            }

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

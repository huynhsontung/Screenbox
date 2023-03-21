using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Models;
using Screenbox.Core.Services;

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

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public async Task FetchArtistsAsync()
        {
            MusicLibraryFetchResult musicLibrary = await _libraryService.FetchMusicAsync();

            GroupedArtists.Clear();
            PopulateGroups();
            foreach (ArtistViewModel artist in musicLibrary.Artists.OrderBy(a => a.Name, StringComparer.CurrentCulture))
            {
                string key = artist == musicLibrary.UnknownArtist
                    ? "\u2026"
                    : MusicPageViewModel.GetFirstLetterGroup(artist.Name);
                GroupedArtists.AddItem(key, artist);
            }
        }

        private void PopulateGroups()
        {
            foreach (string key in MusicPageViewModel.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedArtists.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _refreshTimer.Debounce(() => _ = FetchArtistsAsync(), TimeSpan.FromSeconds(2));
        }
    }
}

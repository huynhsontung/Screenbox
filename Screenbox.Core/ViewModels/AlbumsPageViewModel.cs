using System;
using System.Linq;
using Windows.System;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Models;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels
{
    public sealed class AlbumsPageViewModel : ObservableRecipient
    {
        public ObservableGroupedCollection<string, AlbumViewModel> GroupedAlbums { get; }

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        public AlbumsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            GroupedAlbums = new ObservableGroupedCollection<string, AlbumViewModel>();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public void FetchAlbums()
        {
            // No need to run fetch async. Music page should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();

            GroupedAlbums.Clear();
            PopulateGroups();
            foreach (AlbumViewModel album in musicLibrary.Albums.OrderBy(a => a.Name, StringComparer.CurrentCulture))
            {
                string key = album == musicLibrary.UnknownAlbum
                    ? "\u2026"
                    : MusicPageViewModel.GetFirstLetterGroup(album.Name);
                GroupedAlbums.AddItem(key, album);
            }
        }

        private void PopulateGroups()
        {
            foreach (string key in MusicPageViewModel.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedAlbums.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _refreshTimer.Debounce(FetchAlbums, TimeSpan.FromSeconds(2));
        }
    }
}

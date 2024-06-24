#nullable enable

using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.WinUI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels
{
    public sealed class AlbumsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, AlbumViewModel> GroupedAlbums { get; }

        private readonly ILibraryService _libraryService;
        private readonly IFilesService _filesService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        public AlbumsPageViewModel(ILibraryService libraryService, IFilesService filesService)
        {
            _libraryService = libraryService;
            _filesService = filesService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            GroupedAlbums = new ObservableGroupedCollection<string, AlbumViewModel>();
            PopulateGroups();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public void FetchAlbums()
        {
            // No need to run fetch async. HomePageViewModel should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            Songs = musicLibrary.Songs;

            var groupings = musicLibrary.Albums
                .OrderBy(a => a.Name, StringComparer.CurrentCulture)
                .GroupBy(album => album == musicLibrary.UnknownAlbum
                    ? "\u2026"
                    : MediaGroupingHelpers.GetFirstLetterGroup(album.Name))
                .ToList();
            GroupedAlbums.SyncObservableGroups(groupings);

            // Progressively update when it's still loading
            if (_libraryService.IsLoadingMusic)
            {
                _refreshTimer.Debounce(FetchAlbums, TimeSpan.FromSeconds(5));
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        public async void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;
            if (args.Item is AlbumViewModel album)
            {
                await album.LoadAlbumArtAsync(_filesService);
            }
        }

        private void PopulateGroups()
        {
            foreach (string key in MediaGroupingHelpers.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedAlbums.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(FetchAlbums);
        }
    }
}

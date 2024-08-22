#nullable enable

using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AlbumsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, AlbumViewModel> GroupedAlbums { get; }

        [ObservableProperty]
        private string _sortBy = string.Empty;

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

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
            PropertyChanged += OnPropertyChanged;
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
            IsLoading = _libraryService.IsLoadingMusic;
            Songs = musicLibrary.Songs;

            var groups = GetCurrentGrouping(musicLibrary);
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
            if (_libraryService.IsLoadingMusic)
            {
                _refreshTimer.Debounce(FetchAlbums, TimeSpan.FromSeconds(5));
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        private List<IGrouping<string, AlbumViewModel>> GetDefaultGrouping(MusicLibraryFetchResult musicLibrary)
        {
            var groups = musicLibrary.Albums
                .OrderBy(a => a.Name, StringComparer.CurrentCulture)
                .GroupBy(album => album == musicLibrary.UnknownAlbum
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

        private List<IGrouping<string, AlbumViewModel>> GetArtistGrouping(MusicLibraryFetchResult musicLibrary)
        {
            var groups = musicLibrary.Albums.GroupBy(a => a.ArtistName)
                .OrderBy(g => g.Key, StringComparer.CurrentCulture)
                .ToList();

            var index = groups.FindIndex(g => g.Key == musicLibrary.UnknownArtist.Name);
            if (index >= 0)
            {
                var firstGroup = groups[index];
                groups.RemoveAt(index);
                groups.Insert(0, firstGroup);
            }

            return groups;
        }

        private List<IGrouping<string, AlbumViewModel>> GetYearGrouping(MusicLibraryFetchResult musicLibrary)
        {
            var groups = musicLibrary.Albums.GroupBy(a =>
                    a.Year > 0
                        ? a.Year.ToString()
                        : MediaGroupingHelpers.OtherGroupSymbol)
                .OrderByDescending(g => g.Key == MediaGroupingHelpers.OtherGroupSymbol ? 0 : uint.Parse(g.Key))
                .ToList();
            return groups;
        }

        private List<IGrouping<string, AlbumViewModel>> GetDateAddedGrouping(MusicLibraryFetchResult musicLibrary)
        {
            var groups = musicLibrary.Albums.GroupBy(a => a.DateAdded.Date)
                .OrderByDescending(g => g.Key)
                .Select(g =>
                    new ListGrouping<string, AlbumViewModel>(
                        g.Key == default ? MediaGroupingHelpers.OtherGroupSymbol : g.Key.ToString("d", CultureInfo.CurrentCulture), g))
                .OfType<IGrouping<string, AlbumViewModel>>()
                .ToList();
            return groups;
        }

        private List<IGrouping<string, AlbumViewModel>> GetCurrentGrouping(MusicLibraryFetchResult musicLibrary)
        {
            return SortBy switch
            {
                "artist" => GetArtistGrouping(musicLibrary),
                "year" => GetYearGrouping(musicLibrary),
                "dateAdded" => GetDateAddedGrouping(musicLibrary),
                _ => GetDefaultGrouping(musicLibrary)
            };
        }

        public async void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;
            if (args.Item is AlbumViewModel album)
            {
                await album.LoadAlbumArtAsync(_filesService);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SortBy))
            {
                var groups = GetCurrentGrouping(_libraryService.GetMusicFetchResult());
                GroupedAlbums.Clear();
                foreach (IGrouping<string, AlbumViewModel> group in groups)
                {
                    GroupedAlbums.AddGroup(group);
                }
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(FetchAlbums);
        }

        [RelayCommand]
        private void SetSortBy(string tag)
        {
            SortBy = tag;
        }
    }
}

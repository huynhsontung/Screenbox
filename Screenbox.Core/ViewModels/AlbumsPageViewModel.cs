#nullable enable

using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.WinUI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels
{
    public sealed class AlbumsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, AlbumViewModel> GroupedAlbums { get; }

        private readonly ILibraryService _libraryService;
        private readonly IFilesService _filesService;

        public AlbumsPageViewModel(ILibraryService libraryService, IFilesService filesService) : base(libraryService)
        {
            _libraryService = libraryService;
            _filesService = filesService;
            GroupedAlbums = new ObservableGroupedCollection<string, AlbumViewModel>();
            PropertyChanged += OnPropertyChanged;
        }

        public override void FetchContent()
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
                RefreshTimer.Debounce(FetchContent, TimeSpan.FromSeconds(5));
            }
            else
            {
                RefreshTimer.Stop();
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
            var etcIndex = groups.FindIndex(g => g.Key == MediaGroupingHelpers.OtherGroupSymbol);
            if (etcIndex >= 0)
            {
                var etcGroup = groups[etcIndex];
                groups.RemoveAt(etcIndex);
                groups.Add(etcGroup);
            }

            return groups;
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

        private List<IGrouping<string, AlbumViewModel>> GetCurrentGrouping(MusicLibraryFetchResult musicLibrary)
        {
            return SortBy switch
            {
                "artist" => GetArtistGrouping(musicLibrary),
                "year" => GetYearGrouping(musicLibrary),
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
    }
}

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

namespace Screenbox.Core.ViewModels
{
    public sealed partial class SongsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, MediaViewModel> GroupedSongs { get; }

        [ObservableProperty]
        private string _sortBy = string.Empty;

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        public SongsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            GroupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
            PropertyChanged += OnPropertyChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public void FetchSongs()
        {
            // No need to run fetch async. HomePageViewModel should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            IsLoading = _libraryService.IsLoadingMusic;
            Songs = musicLibrary.Songs;

            // Populate song groups with fetched result
            var groups = GetCurrentGrouping(musicLibrary);
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
            if (_libraryService.IsLoadingMusic)
            {
                _refreshTimer.Debounce(FetchSongs, TimeSpan.FromSeconds(5));
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        private List<IGrouping<string, MediaViewModel>> GetAlbumGrouping(MusicLibraryFetchResult fetchResult)
        {
            var groups = Songs.GroupBy(m => m.Album?.Name ?? fetchResult.UnknownAlbum.Name)
                .OrderBy(g => g.Key)
                .ToList();

            var index = groups.FindIndex(g => g.Key == fetchResult.UnknownAlbum.Name);
            if (index >= 0)
            {
                var firstGroup = groups[index];
                groups.RemoveAt(index);
                groups.Insert(0, firstGroup);
            }

            return groups;
        }

        private List<IGrouping<string, MediaViewModel>> GetArtistGrouping(MusicLibraryFetchResult fetchResult)
        {
            var groups = Songs.GroupBy(m => m.MainArtist?.Name ?? fetchResult.UnknownArtist.Name)
                .OrderBy(g => g.Key)
                .ToList();

            var index = groups.FindIndex(g => g.Key == fetchResult.UnknownArtist.Name);
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

        private List<IGrouping<string, MediaViewModel>> GetCurrentGrouping(MusicLibraryFetchResult musicLibrary)
        {
            return SortBy switch
            {
                "album" => GetAlbumGrouping(musicLibrary),
                "artist" => GetArtistGrouping(musicLibrary),
                "year" => GetYearGrouping(),
                "dateAdded" => GetDateAddedGrouping(),
                _ => GetDefaultGrouping()
            };
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(FetchSongs);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SortBy))
            {
                var groups = GetCurrentGrouping(_libraryService.GetMusicFetchResult());
                GroupedSongs.Clear();
                foreach (IGrouping<string, MediaViewModel> group in groups)
                {
                    GroupedSongs.AddGroup(group);
                }
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

        [RelayCommand]
        private void PlayNext(MediaViewModel media)
        {
            Messenger.SendPlayNext(media);
        }
    }
}

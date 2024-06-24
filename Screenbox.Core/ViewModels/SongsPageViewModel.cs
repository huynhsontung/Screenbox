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
using System.Linq;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class SongsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, MediaViewModel> GroupedSongs { get; }

        [ObservableProperty] private double _groupOverviewItemWidth;

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
            Songs = musicLibrary.Songs;

            // Populate song groups with fetched result
            var groupings = GetGroupings(musicLibrary);
            if (Songs.Count < 5000)
            {
                // Only sync when the number of items is low enough
                // Sync on too many items can cause UI hang
                GroupedSongs.SyncObservableGroups(groupings);
            }
            else
            {
                GroupedSongs.Clear();
                foreach (IGrouping<string, MediaViewModel> grouping in groupings)
                {
                    GroupedSongs.AddGroup(grouping);
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

        private List<IGrouping<string, MediaViewModel>> GetGroupings(MusicLibraryFetchResult fetchResult)
        {
            return SortBy switch
            {
                "album" => Songs.GroupBy(m => m.Album?.Name ?? fetchResult.UnknownAlbum.Name)
                    .OrderBy(g => g.Key == fetchResult.UnknownAlbum.Name)
                    .ThenBy(g => g.Key)
                    .ToList(),
                "artist" => Songs.GroupBy(m => m.MainArtist?.Name ?? fetchResult.UnknownArtist.Name)
                    .OrderBy(g => g.Key == fetchResult.UnknownArtist.Name)
                    .ThenBy(g => g.Key)
                    .ToList(),
                _ => Songs.GroupBy(m => MediaGroupingHelpers.GetFirstLetterGroup(m.Name))
                    .OrderBy(g => g.Key == MediaGroupingHelpers.GroupHeaders.Last().ToString())
                    .ThenBy(g => g.Key, StringComparer.CurrentCulture)
                    .ToList()
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
                FetchSongs();
            }
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

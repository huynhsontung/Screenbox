using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Input;
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
    public sealed partial class SongsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, MediaViewModel> GroupedSongs { get; }

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        public SongsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            GroupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();
            PopulateGroups();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
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
            ShuffleAndPlayCommand.NotifyCanExecuteChanged();

            // Populate song groups with fetched result
            IEnumerable<IGrouping<string, MediaViewModel>> groupings =
                Songs.GroupBy(m => MediaGroupingHelpers.GetFirstLetterGroup(m.Name));
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

        private void PopulateGroups()
        {
            foreach (string key in MediaGroupingHelpers.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedSongs.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(FetchSongs);
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AllVideosPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool _isLoading;

        public ObservableCollection<MediaViewModel> Videos { get; }

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;

        public AllVideosPageViewModel(ILibraryService libraryService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _timer = _dispatcherQueue.CreateTimer();
            _libraryService = libraryService;
            _libraryService.VideosLibraryContentChanged += OnVideosLibraryContentChanged;
            Videos = new ObservableCollection<MediaViewModel>();
        }

        public void UpdateVideos()
        {
            IsLoading = _libraryService.IsLoadingVideos;
            IReadOnlyList<MediaViewModel> videos = _libraryService.GetVideosFetchResult();
            if (videos.Count < 5000)
            {
                // Only sync when the number of items is low enough
                // Sync on too many items can cause UI hang
                Videos.SyncItems(videos);
            }
            else
            {
                Videos.Clear();
                foreach (MediaViewModel video in videos)
                {
                    Videos.Add(video);
                }
            }

            // Progressively update when it's still loading
            if (IsLoading)
            {
                _timer.Debounce(UpdateVideos, TimeSpan.FromSeconds(5));
            }
            else
            {
                _timer.Stop();
            }
        }

        private void OnVideosLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateVideos);
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (Videos.Count == 0) return;
            Messenger.SendQueueAndPlay(media, Videos, true);
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel media)
        {
            Messenger.SendPlayNext(media);
        }
    }
}

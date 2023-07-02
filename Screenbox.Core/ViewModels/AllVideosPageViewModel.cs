using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.System;
using Screenbox.Core.Helpers;

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
            Videos.Clear();
            IReadOnlyList<MediaViewModel> videos = _libraryService.GetVideosFetchResult();
            foreach (MediaViewModel video in videos)
            {
                Videos.Add(video);
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
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.Playlist.Count != Videos.Count || playlist.LastUpdate != Videos)
            {
                Messenger.Send(new ClearPlaylistMessage());
                Messenger.Send(new QueuePlaylistMessage(Videos, false));
            }

            Messenger.Send(new PlayMediaMessage(media, true));
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel media)
        {
            Messenger.SendPlayNext(media);
        }
    }
}

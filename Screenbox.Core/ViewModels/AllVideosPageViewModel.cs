using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AllVideosPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool _isLoading;

        public ObservableCollection<MediaViewModel> Videos { get; }

        private readonly ILibraryService _libraryService;

        public AllVideosPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _libraryService.VideosLibraryContentChanged += OnVideosLibraryContentChanged;
            Videos = new ObservableCollection<MediaViewModel>();
        }

        public void UpdateVideos()
        {
            IsLoading = _libraryService.IsLoadingVideos;
            Videos.Clear();
            IReadOnlyList<MediaViewModel> videos = _libraryService.GetVideosCache();
            foreach (MediaViewModel video in videos)
            {
                Videos.Add(video);
            }
        }

        private void OnVideosLibraryContentChanged(ILibraryService sender, object args)
        {
            UpdateVideos();
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

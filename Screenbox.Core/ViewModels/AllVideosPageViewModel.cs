using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AllVideosPageViewModel : ObservableRecipient
    {
        public ObservableCollection<MediaViewModel> Videos { get; }

        private readonly ILibraryService _libraryService;

        public AllVideosPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            Videos = new ObservableCollection<MediaViewModel>();
        }

        public async Task FetchVideosAsync()
        {
            IReadOnlyList<MediaViewModel> videos = await _libraryService.FetchVideosAsync();
            Videos.Clear();
            foreach (MediaViewModel video in videos)
            {
                Videos.Add(video);
            }
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

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Messages;
using Screenbox.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Screenbox.ViewModels
{
    internal sealed partial class ArtistDetailsPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private ArtistViewModel _source = null!;

        [ObservableProperty] private string _subtext;

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.Playlist.Count != Source.RelatedSongs.Count || playlist.LastUpdate != Source.RelatedSongs)
            {
                Messenger.Send(new ClearPlaylistMessage());
                Messenger.Send(new QueuePlaylistMessage(Source.RelatedSongs, false));
            }

            Messenger.Send(new PlayMediaMessage(media, true));
        }

        [RelayCommand]
        private void ShuffleAndPlay()
        {
            if (Source.RelatedSongs.Count == 0) return;
            Random rnd = new();
            List<MediaViewModel> shuffledList = Source.RelatedSongs.OrderBy(_ => rnd.Next()).ToList();
            Messenger.Send(new ClearPlaylistMessage());
            Messenger.Send(new QueuePlaylistMessage(shuffledList));
            Messenger.Send(new PlayMediaMessage(shuffledList[0], true));
        }
    }
}

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

        public List<IGrouping<AlbumViewModel?, MediaViewModel>>? Albums { get; private set; }

        private List<MediaViewModel>? _itemList;

        public ArtistDetailsPageViewModel()
        {
            _subtext = string.Empty;
        }

        partial void OnSourceChanged(ArtistViewModel value)
        {
            Albums = value.RelatedSongs
                .OrderBy(m => m.MusicProperties?.TrackNumber ?? 0)
                .GroupBy(m => m.Album)
                .OrderByDescending(g => g.Key?.Year ?? 0).ToList();
            Subtext =
                $"{Albums.Count} {Strings.Resources.Albums} • {value.RelatedSongs.Count} {Strings.Resources.Songs}";
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (Albums == null) return;
            _itemList ??= Albums.SelectMany(g => g).ToList();
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.Playlist.Count != _itemList.Count || playlist.LastUpdate != _itemList)
            {
                Messenger.Send(new ClearPlaylistMessage());
                Messenger.Send(new QueuePlaylistMessage(_itemList, false));
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

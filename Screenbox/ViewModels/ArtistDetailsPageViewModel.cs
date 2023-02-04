#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Messages;
using Screenbox.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Converters;

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

        async partial void OnSourceChanged(ArtistViewModel value)
        {
            Albums = value.RelatedSongs
                .OrderBy(m => m.MusicProperties?.TrackNumber ?? 0)
                .GroupBy(m => m.Album)
                .OrderByDescending(g => g.Key?.Year ?? 0).ToList();
            string totalDuration = HumanizedDurationConverter.Convert(GetTotalDuration(value.RelatedSongs));
            Subtext =
                $"{Strings.Resources.AlbumsCount(Albums.Count)} • {Strings.Resources.SongsCount(value.RelatedSongs.Count)} • {Strings.Resources.RunTime(totalDuration)}";

            IEnumerable<Task> loadingTasks = Albums.Where(g => g.Key is { AlbumArt: null })
                .Select(g => g.Key?.LoadAlbumArtAsync())
                .OfType<Task>();
            await Task.WhenAll(loadingTasks);
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

        private static TimeSpan GetTotalDuration(IEnumerable<MediaViewModel> items)
        {
            TimeSpan duration = TimeSpan.Zero;
            foreach (MediaViewModel item in items)
            {
                duration += item.Duration ?? TimeSpan.Zero;
            }

            return duration;
        }
    }
}

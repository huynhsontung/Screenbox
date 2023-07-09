#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class ArtistDetailsPageViewModel : ObservableRecipient
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalDuration))]
        [NotifyPropertyChangedFor(nameof(SongsCount))]
        private ArtistViewModel _source;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AlbumsCount))]
        private List<IGrouping<AlbumViewModel?, MediaViewModel>> _albums;

        public TimeSpan TotalDuration => GetTotalDuration(Source.RelatedSongs);

        public int AlbumsCount => Albums.Count;

        public int SongsCount => Source.RelatedSongs.Count;

        private List<MediaViewModel>? _itemList;

        public ArtistDetailsPageViewModel()
        {
            _source = new ArtistViewModel();
            _albums = new List<IGrouping<AlbumViewModel?, MediaViewModel>>();
        }

        public void OnNavigatedTo(object? parameter)
        {
            Source = parameter switch
            {
                NavigationMetadata { Parameter: ArtistViewModel source } => source,
                ArtistViewModel source => source,
                _ => throw new ArgumentException("Navigation parameter is not an artist")
            };
        }

        async partial void OnSourceChanged(ArtistViewModel value)
        {
            Albums = value.RelatedSongs
                .OrderBy(m => m.MusicProperties?.TrackNumber ?? 0)
                .GroupBy(m => m.Album)
                .OrderByDescending(g => g.Key?.Year ?? 0).ToList();

            IEnumerable<Task> loadingTasks = Albums.Where(g => g.Key is { AlbumArt: null })
                .Select(g => g.Key?.LoadAlbumArtAsync())
                .OfType<Task>();
            await Task.WhenAll(loadingTasks);
        }

        [RelayCommand]
        private void Play(MediaViewModel? media)
        {
            _itemList ??= Albums.SelectMany(g => g).ToList();
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.Playlist.Count != _itemList.Count || playlist.LastUpdate != _itemList)
            {
                Messenger.Send(new ClearPlaylistMessage());
                Messenger.Send(new QueuePlaylistMessage(_itemList, false));
            }

            Messenger.Send(new PlayMediaMessage(media ?? _itemList[0], true));
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

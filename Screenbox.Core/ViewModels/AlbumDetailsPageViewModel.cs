#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AlbumDetailsPageViewModel : ObservableRecipient
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Year))]
        [NotifyPropertyChangedFor(nameof(SongsCount))]
        [NotifyPropertyChangedFor(nameof(TotalDuration))]
        private AlbumViewModel _source = null!;

        public uint? Year => Source.Year;

        public int SongsCount => Source.RelatedSongs.Count;

        public TimeSpan TotalDuration => GetTotalDuration(Source.RelatedSongs);

        public AdvancedCollectionView SortedItems { get; }

        private List<MediaViewModel>? _itemList;

        private class TrackNumberComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                MusicProperties? m1 = x as MusicProperties;
                MusicProperties? m2 = y as MusicProperties;
                uint t1 = m1?.TrackNumber ?? uint.MaxValue;
                uint t2 = m2?.TrackNumber ?? uint.MaxValue;
                return StringComparer.OrdinalIgnoreCase.Compare(t1, t2);
            }
        }

        public AlbumDetailsPageViewModel()
        {
            SortedItems = new AdvancedCollectionView();
            SortedItems.SortDescriptions.Add(new SortDescription(nameof(MediaViewModel.MusicProperties),
                SortDirection.Ascending, new TrackNumberComparer()));
            SortedItems.SortDescriptions.Add(new SortDescription(nameof(MediaViewModel.Name),
                SortDirection.Ascending, StringComparer.CurrentCulture));
        }

        public void OnNavigatedTo(object? parameter)
        {
            Source = parameter switch
            {
                NavigationMetadata { Parameter: AlbumViewModel source } => source,
                AlbumViewModel source => source,
                _ => throw new ArgumentException("Navigation parameter is not an album")
            };
        }

        async partial void OnSourceChanged(AlbumViewModel value)
        {
            SortedItems.Source = value.RelatedSongs;
            if (value.AlbumArt == null)
            {
                await value.LoadAlbumArtAsync();
            }
        }

        [RelayCommand]
        private void Play(MediaViewModel item)
        {
            _itemList ??= SortedItems.OfType<MediaViewModel>().ToList();
            Messenger.SendQueueAndPlay(item, _itemList);
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

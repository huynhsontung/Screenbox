﻿#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public ObservableCollection<MediaViewModel> SortedItems { get; }

        private List<MediaViewModel>? _itemList;
        private readonly IFilesService _filesService;

        public AlbumDetailsPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            SortedItems = new ObservableCollection<MediaViewModel>();
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
            var sorted = value.RelatedSongs.OrderBy(m =>
                    m.MediaInfo.MusicProperties.TrackNumber != 0    // Track number should start with 1
                        ? m.MediaInfo.MusicProperties.TrackNumber
                        : uint.MaxValue)
                .ThenBy(m => m.Name, StringComparer.CurrentCulture);

            SortedItems.Clear();
            foreach (MediaViewModel media in sorted)
            {
                SortedItems.Add(media);
            }

            if (value.AlbumArt == null)
            {
                await value.LoadAlbumArtAsync(_filesService);
            }
        }

        [RelayCommand]
        private void Play(MediaViewModel item)
        {
            _itemList ??= SortedItems.ToList();
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
                duration += item.Duration;
            }

            return duration;
        }
    }
}

﻿#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage.FileProperties;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
using Screenbox.Core;

namespace Screenbox.ViewModels
{
    internal sealed partial class AlbumDetailsPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private AlbumViewModel _source = null!;

        [ObservableProperty] private string _subtext;

        public AdvancedCollectionView SortedItems { get; }

        private List<MediaViewModel>? _itemList;

        private class TrackNumberComparer : IComparer
        {
            public int Compare(object x, object y)
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
            _subtext = string.Empty;
            SortedItems = new AdvancedCollectionView();
            SortedItems.SortDescriptions.Add(new SortDescription(nameof(MediaViewModel.MusicProperties),
                SortDirection.Ascending, new TrackNumberComparer()));
        }

        partial void OnSourceChanged(AlbumViewModel value)
        {
            SortedItems.Source = value.RelatedSongs;
            Subtext = Strings.Resources.SongsCount(value.RelatedSongs.Count);
        }

        [RelayCommand]
        private void Play(MediaViewModel item)
        {
            _itemList ??= SortedItems.OfType<MediaViewModel>().ToList();
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.Playlist.Count != _itemList.Count || playlist.LastUpdate != _itemList)
            {
                Messenger.Send(new ClearPlaylistMessage());
                Messenger.Send(new QueuePlaylistMessage(_itemList, false));
            }

            Messenger.Send(new PlayMediaMessage(item, true));
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

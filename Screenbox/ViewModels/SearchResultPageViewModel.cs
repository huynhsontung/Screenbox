using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;

namespace Screenbox.ViewModels
{
    internal sealed partial class SearchResultPageViewModel : ObservableRecipient
    {
        public string SearchQuery { get; private set; }

        public ObservableCollection<ArtistViewModel> Artists { get; }
        public ObservableCollection<AlbumViewModel> Albums { get; }
        public ObservableCollection<MediaViewModel> Songs { get; }
        public ObservableCollection<MediaViewModel> Videos { get; }

        [ObservableProperty] private bool _showArtists;
        [ObservableProperty] private bool _showAlbums;
        [ObservableProperty] private bool _showSongs;
        [ObservableProperty] private bool _showVideos;

        private SearchResult? _searchResult;

        public SearchResultPageViewModel()
        {
            SearchQuery = string.Empty;
            Artists = new ObservableCollection<ArtistViewModel>();
            Albums = new ObservableCollection<AlbumViewModel>();
            Songs = new ObservableCollection<MediaViewModel>();
            Videos = new ObservableCollection<MediaViewModel>();
        }

        public void Load(SearchResult searchResult)
        {
            _searchResult = searchResult;
            SearchQuery = searchResult.Query;
            if (searchResult.Artists.Count > 0)
            {
                ShowArtists = true;
            }

            if (searchResult.Albums.Count > 0)
            {
                ShowAlbums = true;
            }

            if (searchResult.Songs.Count > 0)
            {
                ShowSongs = true;
                foreach (MediaViewModel song in searchResult.Songs.Take(5))
                {
                    Songs.Add(song);
                }
            }

            if (searchResult.Videos.Count > 0)
            {
                ShowVideos = true;
                foreach (MediaViewModel video in searchResult.Videos.Take(6))
                {
                    Videos.Add(video);
                }
            }
        }

        public void UpdateGridItems(int requestedCount)
        {
            if (_searchResult == null) return;
            SyncCollection(Artists, _searchResult.Artists, requestedCount);
            SyncCollection(Albums, _searchResult.Albums, requestedCount);
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            Messenger.Send(new PlayMediaMessage(media));
        }

        private static void SyncCollection<T>(IList<T> target, IReadOnlyList<T> source, int desiredCount)
        {
            desiredCount = Math.Min(desiredCount, source.Count);
            if (desiredCount <= 0)
            {
                target.Clear();
                return;
            }

            if (target.Count > desiredCount)
            {
                int countToRemove = target.Count - desiredCount;
                for (int i = 0; i < countToRemove; i++)
                {
                    target.RemoveAt(target.Count - 1);
                }
            }
            else
            {
                for (int i = target.Count; i < desiredCount; i++)
                {
                    target.Add(source[i]);
                }
            }
        }
    }
}

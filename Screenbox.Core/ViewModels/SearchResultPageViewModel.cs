#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;

namespace Screenbox.ViewModels
{
    public sealed partial class SearchResultPageViewModel : ObservableRecipient
    {
        public string SearchQuery { get; private set; }

        public SearchResult? SearchResult { get; private set; }

        public ObservableCollection<ArtistViewModel> Artists { get; }
        public ObservableCollection<AlbumViewModel> Albums { get; }
        public ObservableCollection<MediaViewModel> Songs { get; }
        public ObservableCollection<MediaViewModel> Videos { get; }

        [ObservableProperty] private bool _showArtists;
        [ObservableProperty] private bool _showAlbums;
        [ObservableProperty] private bool _showSongs;
        [ObservableProperty] private bool _showVideos;
        [ObservableProperty] private bool _hasMoreArtists;
        [ObservableProperty] private bool _hasMoreAlbums;
        [ObservableProperty] private bool _hasMoreSongs;
        [ObservableProperty] private bool _hasMoreVideos;

        private readonly INavigationService _navigationService;

        public SearchResultPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            SearchQuery = string.Empty;
            Artists = new ObservableCollection<ArtistViewModel>();
            Albums = new ObservableCollection<AlbumViewModel>();
            Songs = new ObservableCollection<MediaViewModel>();
            Videos = new ObservableCollection<MediaViewModel>();
        }

        public void Load(SearchResult searchResult)
        {
            SearchResult = searchResult;
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

            UpdateHasMoreProperties(searchResult);
        }

        public void UpdateGridItems(int requestedCount)
        {
            if (SearchResult == null) return;
            SyncCollection(Artists, SearchResult.Artists, requestedCount);
            SyncCollection(Albums, SearchResult.Albums, requestedCount);
            UpdateHasMoreProperties(SearchResult);
        }

        private void UpdateHasMoreProperties(SearchResult searchResult)
        {
            HasMoreArtists = Artists.Count < searchResult.Artists.Count;
            HasMoreAlbums = Albums.Count < searchResult.Albums.Count;
            HasMoreSongs = Songs.Count < searchResult.Songs.Count;
            HasMoreVideos = Videos.Count < searchResult.Videos.Count;
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            Messenger.Send(new PlayMediaMessage(media));
        }

        [RelayCommand]
        private void PlaySong(MediaViewModel media)
        {
            if (SearchResult == null) return;
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.Playlist.Count != SearchResult.Songs.Count || playlist.LastUpdate != SearchResult.Songs)
            {
                Messenger.Send(new ClearPlaylistMessage());
                Messenger.Send(new QueuePlaylistMessage(SearchResult.Songs, false));
            }

            Messenger.Send(new PlayMediaMessage(media, true));
        }

        [RelayCommand(CanExecute = nameof(HasMoreArtists))]
        private void SeeAllArtists()
        {
            _navigationService.Navigate(typeof(ArtistSearchResultPageViewModel), this);
        }

        [RelayCommand(CanExecute = nameof(HasMoreAlbums))]
        private void SeeAllAlbums()
        {
            _navigationService.Navigate(typeof(AlbumSearchResultPageViewModel), this);
        }

        [RelayCommand(CanExecute = nameof(HasMoreSongs))]
        private void SeeAllSongs()
        {
            _navigationService.Navigate(typeof(SongSearchResultPageViewModel), this);
        }

        [RelayCommand(CanExecute = nameof(HasMoreVideos))]
        private void SeeAllVideos()
        {
            _navigationService.Navigate(typeof(VideoSearchResultPageViewModel), this);
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

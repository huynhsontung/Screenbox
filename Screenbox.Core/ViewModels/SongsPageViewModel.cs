using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class SongsPageViewModel : ObservableRecipient
    {
        public ObservableGroupedCollection<string, MediaViewModel> GroupedSongs { get; }

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;
        private IReadOnlyList<MediaViewModel> _songs;

        public SongsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            _songs = Array.Empty<MediaViewModel>();
            GroupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public void FetchSongs()
        {
            // No need to run fetch async. Music page should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            _songs = musicLibrary.Songs.OrderBy(m => m.Name, StringComparer.CurrentCulture).ToList();

            // Populate song groups with fetched result
            GroupedSongs.Clear();
            PopulateGroups();
            foreach (MediaViewModel song in _songs)
            {
                GroupedSongs.AddItem(MusicPageViewModel.GetFirstLetterGroup(song.Name), song);
            }

            // Progressively update when it's still loading
            if (_libraryService.IsLoadingMusic)
            {
                _refreshTimer.Debounce(FetchSongs, TimeSpan.FromSeconds(5));
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        private void PopulateGroups()
        {
            foreach (string key in MusicPageViewModel.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedSongs.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(FetchSongs);
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (_songs.Count == 0) return;
            PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
            if (playlist.Playlist.Count != _songs.Count || playlist.LastUpdate != _songs)
            {
                Messenger.Send(new ClearPlaylistMessage());
                Messenger.Send(new QueuePlaylistMessage(_songs, false));
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

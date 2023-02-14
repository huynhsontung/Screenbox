using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core;
using Screenbox.Services;
using Windows.System;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;

namespace Screenbox.ViewModels
{
    internal sealed partial class AlbumsPageViewModel : ObservableRecipient
    {
        public ObservableGroupedCollection<string, AlbumViewModel> GroupedAlbums { get; }

        private bool HasSongs => _songs.Count > 0;

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;
        private IReadOnlyList<MediaViewModel> _songs;

        public AlbumsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            _songs = Array.Empty<MediaViewModel>();
            GroupedAlbums = new ObservableGroupedCollection<string, AlbumViewModel>();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public async Task FetchAlbumsAsync()
        {
            MusicLibraryFetchResult musicLibrary = await _libraryService.FetchMusicAsync();
            _songs = musicLibrary.Songs;

            GroupedAlbums.Clear();
            PopulateGroups();
            foreach (AlbumViewModel album in musicLibrary.Albums.OrderBy(a => a.Name, StringComparer.CurrentCulture))
            {
                string key = album == musicLibrary.UnknownAlbum
                    ? "\u2026"
                    : MusicPageViewModel.GetFirstLetterGroup(album.Name);
                GroupedAlbums.AddItem(key, album);
            }

            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
        }

        private void PopulateGroups()
        {
            foreach (string key in MusicPageViewModel.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedAlbums.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _refreshTimer.Debounce(() => _ = FetchAlbumsAsync(), TimeSpan.FromSeconds(2));
        }

        [RelayCommand(CanExecute = nameof(HasSongs))]
        private void ShuffleAndPlay()
        {
            if (_songs.Count == 0) return;
            Random rnd = new();
            List<MediaViewModel> shuffledList = _songs.OrderBy(_ => rnd.Next()).ToList();
            Messenger.Send(new ClearPlaylistMessage());
            Messenger.Send(new QueuePlaylistMessage(shuffledList));
            Messenger.Send(new PlayMediaMessage(shuffledList[0], true));
        }
    }
}

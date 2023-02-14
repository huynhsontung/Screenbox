using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Windows.System;
using CommunityToolkit.Mvvm.Messaging;

namespace Screenbox.ViewModels
{
    internal sealed partial class ArtistsPageViewModel : ObservableRecipient
    {
        public ObservableGroupedCollection<string, ArtistViewModel> GroupedArtists { get; }

        private bool HasSongs => _songs.Count > 0;

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;
        private IReadOnlyList<MediaViewModel> _songs;

        public ArtistsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            _songs = Array.Empty<MediaViewModel>();
            GroupedArtists = new ObservableGroupedCollection<string, ArtistViewModel>();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public async Task FetchArtistsAsync()
        {
            MusicLibraryFetchResult musicLibrary = await _libraryService.FetchMusicAsync();
            _songs = musicLibrary.Songs;

            GroupedArtists.Clear();
            PopulateGroups();
            foreach (ArtistViewModel artist in musicLibrary.Artists.OrderBy(a => a.Name, StringComparer.CurrentCulture))
            {
                string key = artist == musicLibrary.UnknownArtist
                    ? "\u2026"
                    : MusicPageViewModel.GetFirstLetterGroup(artist.Name);
                GroupedArtists.AddItem(key, artist);
            }

            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
        }

        private void PopulateGroups()
        {
            foreach (string key in MusicPageViewModel.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedArtists.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _refreshTimer.Debounce(() => _ = FetchArtistsAsync(), TimeSpan.FromSeconds(2));
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Services;
using Windows.System;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Controls;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Collections;

namespace Screenbox.ViewModels
{
    internal sealed partial class SongsPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private ObservableGroupedCollection<string,MediaViewModel> _groupedSongs;
        [ObservableProperty] private bool _hasSongs;

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private IReadOnlyList<MediaViewModel> _songs;

        public SongsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _groupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _songs = Array.Empty<MediaViewModel>();

            PopulateGroups();
        }

        public async Task FetchSongsAsync()
        {
            if (_songs.Count > 0) return;
            MusicLibraryFetchResult musicLibrary = await _libraryService.FetchMusicAsync();
            _songs = musicLibrary.Songs.OrderBy(m => m.Name, StringComparer.CurrentCulture).ToList();
            HasSongs = _songs.Count > 0;
            foreach (MediaViewModel song in _songs)
            {
                GroupSongsByName(song);
            }

            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
            PlayCommand.NotifyCanExecuteChanged();
        }

        private void GroupSongsByName(MediaViewModel song)
        {
            GroupedSongs.AddItem(MusicPageViewModel.GetFirstLetterGroup(song.Name), song);
        }

        private void PopulateGroups()
        {
            foreach (string key in MusicPageViewModel.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedSongs.AddGroup(key);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSongs))]
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

        [RelayCommand]
        private async Task ShowPropertiesAsync(MediaViewModel media)
        {
            ContentDialog propertiesDialog = PropertiesView.GetDialog(media);
            await propertiesDialog.ShowAsync();
        }
    }
}

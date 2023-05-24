#nullable enable

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
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool _isLoading;

        [ObservableProperty] private bool _hasContent;

        public int Count => _songs.Count;

        private bool HasSongs => _songs.Count > 0;

        private bool LibraryLoaded => _libraryService.MusicLibrary != null;

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;
        private List<MediaViewModel> _songs;

        public MusicPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
            _songs = new List<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _timer = _dispatcherQueue.CreateTimer();
            _hasContent = true;
        }

        public void UpdateSongs()
        {
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            _songs = new List<MediaViewModel>(musicLibrary.Songs);
            IsLoading = _libraryService.IsLoadingMusic;
            HasContent = _songs.Count > 0 || IsLoading;
            AddFolderCommand.NotifyCanExecuteChanged();
            ShuffleAndPlayCommand.NotifyCanExecuteChanged();

            if (IsLoading)
            {
                _timer.Debounce(UpdateSongs, TimeSpan.FromSeconds(5));
            }
            else
            {
                _timer.Stop();
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateSongs);
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

        [RelayCommand(CanExecute = nameof(LibraryLoaded))]
        private async Task AddFolder()
        {
            StorageFolder? addedFolder = await _libraryService.MusicLibrary?.RequestAddFolderAsync();
            if (addedFolder != null)
            {
                _timer.Debounce(() => IsLoading = _libraryService.IsLoadingMusic, TimeSpan.FromSeconds(1));
            }
        }
    }
}

#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool _isLoading;

        public const string GroupHeaders = "&#ABCDEFGHIJKLMNOPQRSTUVWXYZ\u2026";

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
        }

        public void UpdateSongs()
        {
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            _songs = new List<MediaViewModel>(musicLibrary.Songs);
            IsLoading = _libraryService.IsLoadingMusic;
            AddFolderCommand.NotifyCanExecuteChanged();
            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
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
            await _libraryService.MusicLibrary?.RequestAddFolderAsync();
        }

        public static string GetFirstLetterGroup(string name)
        {
            char letter = char.ToUpper(name[0], CultureInfo.CurrentCulture);
            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(letter))
                return letter.ToString();
            if (char.IsNumber(letter)) return "#";
            if (char.IsSymbol(letter) || char.IsPunctuation(letter) || char.IsSeparator(letter)) return "&";
            return "\u2026";
        }
    }
}

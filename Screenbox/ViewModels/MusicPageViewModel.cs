#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core;

namespace Screenbox.ViewModels
{
    internal sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool _isLoading;

        public const string GroupHeaders = "&#ABCDEFGHIJKLMNOPQRSTUVWXYZ\u2026";

        public int Count => _songs.Count;

        private bool HasSongs => _songs.Count > 0;

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;
        private readonly List<MediaViewModel> _songs;
        private StorageLibrary? _library;

        public MusicPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _songs = new List<MediaViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _timer = _dispatcherQueue.CreateTimer();
        }

        public async Task FetchMusicAsync()
        {
            _timer.Debounce(() => IsLoading = true, TimeSpan.FromMilliseconds(200));

            MusicLibraryFetchResult music = await _libraryService.FetchMusicAsync();
            _songs.Clear();
            _songs.AddRange(music.Songs);

            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
            _timer.Stop();
            IsLoading = false;
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
        private async Task AddFolder()
        {
            _library ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            await _library?.RequestAddFolderAsync();
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

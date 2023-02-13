#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Controls;
using Screenbox.Core;

namespace Screenbox.ViewModels
{
    internal sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private ObservableGroupedCollection<string, ArtistViewModel> _groupedArtists;
        [ObservableProperty] private bool _isLoading;

        public const string GroupHeaders = "&#ABCDEFGHIJKLMNOPQRSTUVWXYZ\u2026";

        public string NavigationState { get; set; }

        public int Count => _songs.Count;

        public bool IsLoaded => _library != null;

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;
        private readonly object _lockObject;
        private readonly List<MediaViewModel> _songs;
        private readonly HashSet<string> _artistNames;
        private Task _loadSongsTask;
        private StorageLibrary? _library;

        public MusicPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _loadSongsTask = Task.CompletedTask;
            _lockObject = new object();
            _groupedArtists = new ObservableGroupedCollection<string, ArtistViewModel>();
            _songs = new List<MediaViewModel>();
            _artistNames = new HashSet<string>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _timer = _dispatcherQueue.CreateTimer();
            NavigationState = string.Empty;

            PopulateGroups();
        }

        public Task FetchSongsAsync()
        {
            lock (_lockObject)
            {
                if (!_loadSongsTask.IsCompleted)
                {
                    return _loadSongsTask;
                }

                return _loadSongsTask = FetchSongsInternalAsync();
            }
        }

        private bool HasSongs()
        {
            return _songs.Count != 0;
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
        private async Task AddFolder()
        {
            if (_library == null) return;
            await _library.RequestAddFolderAsync();
        }

        [RelayCommand]
        private async Task ShowPropertiesAsync(MediaViewModel media)
        {
            ContentDialog propertiesDialog = PropertiesView.GetDialog(media);
            await propertiesDialog.ShowAsync();
        }

        private async Task FetchSongsInternalAsync()
        {
            if (_songs.Count > 0) return;
            _timer.Debounce(() => IsLoading = true, TimeSpan.FromMilliseconds(200));
            await InitializeLibraryAsync();
            await FetchAndProcessSongsAsync();
            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
            PlayCommand.NotifyCanExecuteChanged();
            _timer.Stop();
            IsLoading = false;
        }

        private async Task InitializeLibraryAsync()
        {
            if (_library == null)
            {
                _library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                _library.DefinitionChanged += LibraryOnDefinitionChanged;
            }
        }

        private async Task FetchAndProcessSongsAsync()
        {
            MusicLibraryFetchResult music = await _libraryService.FetchMusicAsync();
            _songs.AddRange(music.Songs);

            foreach (MediaViewModel song in music.Songs)
            {
                GroupArtistsByName(song);
            }
        }

        private void GroupArtistsByName(MediaViewModel song)
        {
            if (song.Artists.Length == 0)
                return;

            foreach (ArtistViewModel artist in song.Artists)
            {
                if (_artistNames.Contains(artist.Name))
                    continue;

                string key = artist.Name != Strings.Resources.UnknownArtist
                    ? GetFirstLetterGroup(artist.Name)
                    : "\u2026";
                GroupedArtists.AddItem(key, artist);
                _artistNames.Add(artist.Name);
            }
        }

        private async void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            if (!_loadSongsTask.IsCompleted)
            {
                await _loadSongsTask;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                _songs.Clear();
                FetchSongsAsync();
            });
        }

        private void PopulateGroups()
        {
            // TODO: Support other languages beside English
            const string letters = "&#ABCDEFGHIJKLMNOPQRSTUVWXYZ\u2026";
            foreach (string key in letters.Select(letter => letter.ToString()))
            {
                GroupedArtists.AddGroup(key);
            }
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

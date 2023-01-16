#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Factories;
using Screenbox.Services;
using Screenbox.Controls;
using Screenbox.Core;

namespace Screenbox.ViewModels
{
    internal sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private ObservableGroupedCollection<string, MediaViewModel> _groupedSongs;
        [ObservableProperty] private ObservableGroupedCollection<string, AlbumViewModel> _groupedAlbums;
        [ObservableProperty] private ObservableGroupedCollection<string, ArtistViewModel> _groupedArtists;
        [ObservableProperty] private bool _isLoading;

        public int Count => _songs.Count;

        public bool IsLoaded => _library != null;

        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly object _lockObject;
        private readonly List<MediaViewModel> _songs;
        private readonly HashSet<string> _albumNames;
        private readonly HashSet<string> _artistNames;
        private Task _loadSongsTask;
        private StorageLibrary? _library;

        public MusicPageViewModel(IFilesService filesService,
            MediaViewModelFactory mediaFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _loadSongsTask = Task.CompletedTask;
            _lockObject = new object();
            _groupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();
            _groupedAlbums = new ObservableGroupedCollection<string, AlbumViewModel>();
            _groupedArtists = new ObservableGroupedCollection<string, ArtistViewModel>();
            _songs = new List<MediaViewModel>();
            _albumNames = new HashSet<string>();
            _artistNames = new HashSet<string>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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
            const int maxCount = 5000;

            if (_songs.Count > 0) return;
            if (_library == null)
            {
                _library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                _library.DefinitionChanged += LibraryOnDefinitionChanged;
            }

            StorageFileQueryResult queryResult = _filesService.GetSongsFromLibrary();
            uint fetchIndex = 0;
            IsLoading = true;
            while (fetchIndex < maxCount)
            {
                IReadOnlyList<StorageFile> files = await queryResult.GetFilesAsync(fetchIndex, 50);
                if (files.Count == 0) break;
                fetchIndex += (uint)files.Count;

                List<MediaViewModel> songs = files.Select(_mediaFactory.GetSingleton).ToList();
                _songs.AddRange(songs);
                await Task.WhenAll(songs.Select(vm => vm.LoadDetailsAsync()));

                foreach (MediaViewModel song in songs)
                {
                    GroupedSongs.AddItem(GetFirstLetterGroup(song.Name), song);

                    if (song.Album != null && !_albumNames.Contains(song.Album.ToString()))
                    {
                        string albumName = song.Album.Name;
                        string key = albumName != Strings.Resources.UnknownAlbum
                            ? GetFirstLetterGroup(albumName)
                            : "\u2026";
                        GroupedAlbums.AddItem(key, song.Album);
                        _albumNames.Add(song.Album.ToString());
                    }

                    if (song.Artists?.Length > 0)
                    {
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
                }
            }

            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
            PlayCommand.NotifyCanExecuteChanged();
            IsLoading = false;
        }

        private async void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            if (!_loadSongsTask.IsCompleted)
            {
                await _loadSongsTask;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                GroupedSongs.Clear();
                _songs.Clear();
                FetchSongsAsync();
            });
        }

        private string GetFirstLetterGroup(string name)
        {
            char letter = char.ToUpper(name[0], CultureInfo.CurrentCulture);
            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(letter))
                return letter.ToString();
            if (char.IsNumber(letter)) return "#";
            if (char.IsSymbol(letter) || char.IsPunctuation(letter) || char.IsSeparator(letter)) return "&";
            return "\u2026";
        }

        private void PopulateGroups()
        {
            // TODO: Support other languages beside English
            const string letters = "&#ABCDEFGHIJKLMNOPQRSTUVWXYZ\u2026";
            foreach (string key in letters.Select(letter => letter.ToString()))
            {
                GroupedSongs.AddGroup(key);
                GroupedAlbums.AddGroup(key);
                GroupedArtists.AddGroup(key);
            }
        }
    }
}

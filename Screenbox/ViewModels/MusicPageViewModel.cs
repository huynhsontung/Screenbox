#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Factories;
using Screenbox.Services;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Controls;
using Screenbox.Core;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;

namespace Screenbox.ViewModels
{
    internal sealed partial class MusicPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private ObservableGroupedCollection<string, MediaViewModel> _groupedSongs;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly object _lockObject;
        private readonly List<MediaViewModel> _songs;
        private Task _loadSongsTask;
        private StorageLibrary? _library;

        public MusicPageViewModel(IFilesService filesService,
            MediaViewModelFactory mediaFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            _loadSongsTask = Task.CompletedTask;
            _lockObject = new object();
            _groupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();
            _songs = new List<MediaViewModel>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
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
            while (fetchIndex < maxCount)
            {
                IReadOnlyList<StorageFile> files = await queryResult.GetFilesAsync(fetchIndex, 50);
                if (files.Count == 0) break;
                fetchIndex += (uint)files.Count;

                List<MediaViewModel> songs = files.Select(_mediaFactory.GetSingleton).ToList();
                _songs.AddRange(songs);
                await Task.WhenAll(songs.Select(vm => vm.LoadTitleAsync()));

                foreach (MediaViewModel song in songs)
                {
                    GroupedSongs.AddItem(GetFirstLetterGroup(song.Name), song);
                }
            }

            ShuffleAndPlayCommand.NotifyCanExecuteChanged();
            PlayCommand.NotifyCanExecuteChanged();
        }

        private async void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            if (!_loadSongsTask.IsCompleted)
            {
                await _loadSongsTask;
            }

            GroupedSongs.Clear();
            _songs.Clear();
            await FetchSongsAsync();
        }

        private string GetFirstLetterGroup(string name)
        {
            if (char.IsLetter(name, 0)) return name.Substring(0, 1).ToUpper();
            return char.IsNumber(name, 0) ? "#" : "&";
        }
    }
}

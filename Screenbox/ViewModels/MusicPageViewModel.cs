#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Factories;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private ObservableGroupedCollection<string, MediaViewModel> _groupedSongs;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly IFilesService _filesService;
        private readonly INavigationService _navigationService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly object _lockObject;
        private readonly List<MediaViewModel> _songs;
        private Task _loadSongsTask;
        private StorageLibrary? _library;

        public MusicPageViewModel(IFilesService filesService,
            INavigationService navigationService,
            MediaViewModelFactory mediaFactory)
        {
            _filesService = filesService;
            _navigationService = navigationService;
            _mediaFactory = mediaFactory;
            _navigationViewDisplayMode = navigationService.DisplayMode;
            _loadSongsTask = Task.CompletedTask;
            _lockObject = new object();
            _groupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();
            _songs = new List<MediaViewModel>();

            navigationService.DisplayModeChanged += NavigationServiceOnDisplayModeChanged;

            // Activate the view model's messenger
            IsActive = true;
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

        private void NavigationServiceOnDisplayModeChanged(object sender, NavigationServiceDisplayModeChangedEventArgs e)
        {
            NavigationViewDisplayMode = e.NewValue;
        }

        private bool HasSongs()
        {
            return _songs.Count != 0;
        }

        [RelayCommand(CanExecute = nameof(HasSongs))]
        private void Play(MediaViewModel media)
        {
            if (_songs.Count == 0) return;
            Messenger.Send(new QueuePlaylistMessage(_songs, media));
        }

        [RelayCommand(CanExecute = nameof(HasSongs))]
        private void ShuffleAndPlay()
        {
            if (_songs.Count == 0) return;
            Random rnd = new();
            List<MediaViewModel> shuffledList = _songs.OrderBy(_ => rnd.Next()).ToList();
            Messenger.Send(new QueuePlaylistMessage(shuffledList, shuffledList[0]));
        }

        [RelayCommand]
        private async Task AddFolder()
        {
            if (_library == null) return;
            await _library.RequestAddFolderAsync();
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

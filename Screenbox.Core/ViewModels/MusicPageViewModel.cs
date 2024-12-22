#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool _hasContent;
        [ObservableProperty] private int _songsCount;
        [ObservableProperty] private int _albumsCount;
        [ObservableProperty] private int _artistsCount;

        private bool LibraryLoaded => _libraryService.MusicLibrary != null;

        private readonly ILibraryService _libraryService;
        private readonly IResourceService _resourceService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        public MusicPageViewModel(ILibraryService libraryService, IResourceService resourceService)
        {
            _libraryService = libraryService;
            _resourceService = resourceService;
            _libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            _hasContent = true;
        }

        public void UpdateSongs()
        {
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            HasContent = musicLibrary.Songs.Count > 0 || _libraryService.IsLoadingMusic;
            SongsCount = musicLibrary.Songs.Count;
            AlbumsCount = musicLibrary.Albums.Count;
            ArtistsCount = musicLibrary.Artists.Count;
            AddFolderCommand.NotifyCanExecuteChanged();

            if (_libraryService.IsLoadingMusic)
            {
                _refreshTimer.Debounce(UpdateSongs, TimeSpan.FromSeconds(5));
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateSongs);
        }

        [RelayCommand(CanExecute = nameof(LibraryLoaded))]
        private async Task AddFolder()
        {
            try
            {
                await _libraryService.MusicLibrary?.RequestAddFolderAsync();
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    _resourceService.GetString(ResourceName.FailedToAddFolderNotificationTitle), e.Message));
            }
        }
    }
}

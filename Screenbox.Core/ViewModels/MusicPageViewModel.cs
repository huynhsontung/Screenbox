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
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool _isLoading;

        [ObservableProperty] private bool _hasContent;

        private bool LibraryLoaded => _libraryService.MusicLibrary != null;

        private readonly ILibraryService _libraryService;
        private readonly IResourceService _resourceService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;
        private List<MediaViewModel> _songs;

        public MusicPageViewModel(ILibraryService libraryService, IResourceService resourceService)
        {
            _libraryService = libraryService;
            _resourceService = resourceService;
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

        [RelayCommand(CanExecute = nameof(LibraryLoaded))]
        private async Task AddFolder()
        {
            StorageFolder? addedFolder;
            try
            {
                addedFolder = await _libraryService.MusicLibrary?.RequestAddFolderAsync();
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    _resourceService.GetString(ResourceName.FailedToAddFolderNotificationTitle), e.Message));
                return;
            }

            if (addedFolder != null)
            {
                _timer.Debounce(() => IsLoading = _libraryService.IsLoadingMusic, TimeSpan.FromSeconds(1));
            }
        }
    }
}

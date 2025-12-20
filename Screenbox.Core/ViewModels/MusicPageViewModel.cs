#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
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

        private bool LibraryLoaded => _libraryContext.MusicLibrary != null;

        private readonly LibraryContext _libraryContext;
        private readonly ILibraryService _libraryService;
        private readonly IResourceService _resourceService;
        private readonly DispatcherQueue _dispatcherQueue;

        public MusicPageViewModel(LibraryContext libraryContext, ILibraryService libraryService, IResourceService resourceService)
        {
            _libraryContext = libraryContext;
            _libraryService = libraryService;
            _resourceService = resourceService;
            _libraryContext.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _hasContent = true;
        }

        public void UpdateSongs()
        {
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult(_libraryContext);
            HasContent = musicLibrary.Songs.Count > 0 || _libraryContext.IsLoadingMusic;
            AddFolderCommand.NotifyCanExecuteChanged();
        }

        private void OnMusicLibraryContentChanged(LibraryContext sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateSongs);
        }

        [RelayCommand(CanExecute = nameof(LibraryLoaded))]
        private async Task AddFolder()
        {
            try
            {
                await _libraryContext.MusicLibrary?.RequestAddFolderAsync();
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    _resourceService.GetString(ResourceName.FailedToAddFolderNotificationTitle), e.Message));
            }
        }
    }
}

#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using IResourceService = Screenbox.Core.Services.IResourceService;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PlayQueuePageViewModel : ObservableRecipient
    {
        private readonly IFilesService _filesService;
        private readonly IResourceService _resourceService;
        private readonly MediaViewModelFactory _mediaFactory;

        public PlayQueuePageViewModel(IFilesService filesService, IResourceService resourceService, MediaViewModelFactory mediaFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _resourceService = resourceService;
        }

        public void AddUrl(Uri uri)
        {
            MediaViewModel media = _mediaFactory.GetTransient(uri);
            Messenger.Send(new QueuePlaylistMessage(new[] { media }));
        }

        [RelayCommand]
        private async Task AddFolderAsync()
        {
            try
            {
                StorageFolder? folder = await _filesService.PickFolderAsync();
                if (folder == null) return;
                IReadOnlyList<IStorageItem> items = await _filesService.GetSupportedItems(folder).GetItemsAsync();
                MediaViewModel[] files = items.OfType<IStorageFile>().Select(f => _mediaFactory.GetSingleton(f)).ToArray();
                if (files.Length == 0) return;
                Messenger.Send(new QueuePlaylistMessage(files));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    _resourceService.GetString(ResourceName.FailedToOpenFilesNotificationTitle), e.Message));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlayQueuePageViewModel : ObservableRecipient
{
    private readonly IFilesService _filesService;
    private readonly IPlayQueueCoordinator _coordinator;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IMediaListFactory _mediaListFactory;

    public PlayQueuePageViewModel(
        IFilesService filesService,
        IPlayQueueCoordinator coordinator,
        MediaViewModelFactory mediaFactory,
        IMediaListFactory mediaListFactory)
    {
        _filesService = filesService;
        _coordinator = coordinator;
        _mediaFactory = mediaFactory;
        _mediaListFactory = mediaListFactory;
    }

    [RelayCommand]
    private void AddUrl(Uri? uri)
    {
        if (uri is null) return;
        Messenger.Send(new PlayMediaMessage(_mediaFactory.Create(uri)));
    }

    /// <summary>
    /// Opens a folder picker and queues all supported media files in the selected folder.
    /// </summary>
    [RelayCommand]
    private async Task AddFolderAsync()
    {
        try
        {
            StorageFolder? folder = await _filesService.PickFolderAsync();
            if (folder == null) return;
            IReadOnlyList<IStorageItem> items = await _filesService.GetSupportedItems(folder).GetItemsAsync();
            var mediaList = await _mediaListFactory.TryParseMediaListAsync(items);
            if (mediaList?.Items.Count > 0)
            {
                _coordinator.Enqueue(mediaList.Items);
            }
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.FileOpenFailed, message: e.Message));
        }
    }
}

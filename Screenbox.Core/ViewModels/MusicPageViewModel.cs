#nullable enable

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class MusicPageViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<MusicLibrary>>
{
    [ObservableProperty] private bool _hasContent;

    private bool LibraryLoaded => _libraryContext.StorageMusicLibrary != null;

    private readonly LibraryContext _libraryContext;
    private readonly IResourceService _resourceService;
    private readonly DispatcherQueue _dispatcherQueue;

    public MusicPageViewModel(LibraryContext libraryContext, IResourceService resourceService)
    {
        _libraryContext = libraryContext;
        _resourceService = resourceService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _hasContent = true;

        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<MusicLibrary> message)
    {
        if (message.Sender is not LibraryContext) return;
        _dispatcherQueue.TryEnqueue(UpdateSongs);
    }

    public void UpdateSongs()
    {
        HasContent = _libraryContext.MusicLibrary.Songs.Count > 0 || _libraryContext.IsLoadingMusic;
        AddFolderCommand.NotifyCanExecuteChanged();
    }


    [RelayCommand(CanExecute = nameof(LibraryLoaded))]
    private async Task AddFolder()
    {
        try
        {
            await _libraryContext.StorageMusicLibrary?.RequestAddFolderAsync();
        }
        catch (Exception e)
        {
            Messenger.Send(new ErrorMessage(
                _resourceService.GetString(ResourceName.FailedToAddFolderNotificationTitle), e.Message));
        }
    }
}

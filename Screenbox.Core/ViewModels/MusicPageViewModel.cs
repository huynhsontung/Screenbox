#nullable enable

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class MusicPageViewModel : ObservableRecipient,
    IRecipient<LibraryContentChangedMessage>
{
    [ObservableProperty] private bool _hasContent;

    private bool LibraryLoaded => _libraryContext.MusicLibrary != null;

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

    public void Receive(LibraryContentChangedMessage message)
    {
        if (message.LibraryId != KnownLibraryId.Music) return;
        _dispatcherQueue.TryEnqueue(UpdateSongs);
    }

    public void UpdateSongs()
    {
        HasContent = _libraryContext.Songs.Count > 0 || _libraryContext.IsLoadingMusic;
        AddFolderCommand.NotifyCanExecuteChanged();
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

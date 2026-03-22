#nullable enable

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class MusicPageViewModel : ObservableRecipient,
    IRecipient<LibraryContentChangedMessage>
{
    [ObservableProperty] private bool _hasContent;

    /// <summary>Gets a value indicating whether the Music library is available, used to enable the add-folder command.</summary>
    public bool LibraryLoaded => _libraryContext.MusicLibrary != null;

    private readonly LibraryContext _libraryContext;
    private readonly DispatcherQueue _dispatcherQueue;

    public MusicPageViewModel(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
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

    /// <summary>
    /// Requests adding a new folder to the Music library.
    /// Sends a <see cref="Core.Messages.FailedToAddFolderNotificationMessage"/> on failure.
    /// </summary>
    [RelayCommand(CanExecute = nameof(LibraryLoaded))]
    private async Task AddFolderAsync()
    {
        try
        {
            await _libraryContext.MusicLibrary?.RequestAddFolderAsync();
        }
        catch (Exception e)
        {
            Messenger.Send(new FailedToAddFolderNotificationMessage(e.Message));
        }
    }

}


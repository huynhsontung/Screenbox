#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Navigation;

namespace Screenbox.Core.ViewModels;

public sealed partial class VideosPageViewModel : ObservableRecipient,
    IRecipient<LibraryContentChangedMessage>
{
    public ObservableCollection<StorageFolder> Breadcrumbs { get; }

    [ObservableProperty] private bool _hasVideos;

    /// <summary>Gets a value indicating whether the Videos library is available, used to enable the add-folder command.</summary>
    public bool HasLibrary => _libraryContext.VideosLibrary != null;

    private readonly LibraryContext _libraryContext;
    private readonly DispatcherQueue _dispatcherQueue;

    public VideosPageViewModel(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
        _hasVideos = true;
        Breadcrumbs = new ObservableCollection<StorageFolder>();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        IsActive = true;
    }

    public void Receive(LibraryContentChangedMessage message)
    {
        if (message.LibraryId != KnownLibraryId.Videos) return;
        _dispatcherQueue.TryEnqueue(UpdateVideos);
    }

    public void UpdateVideos()
    {
        if (Breadcrumbs.Count == 0 && TryGetFirstFolder(out StorageFolder firstFolder))
            Breadcrumbs.Add(firstFolder);
        HasVideos = _libraryContext.Videos.Count > 0;
        OnPropertyChanged(nameof(HasLibrary));
    }

    public void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        IReadOnlyList<StorageFolder>? crumbs = e.Parameter as IReadOnlyList<StorageFolder>;
        UpdateBreadcrumbs(crumbs);
    }

    private bool TryGetFirstFolder(out StorageFolder folder)
    {
        try
        {
            folder = SystemInformation.IsXbox ? KnownFolders.RemovableDevices : KnownFolders.VideosLibrary;
            return true;
        }
        catch (Exception e)
        {
            folder = ApplicationData.Current.TemporaryFolder;
            Messenger.Send(new ErrorMessage(null, e.Message));
            LogService.Log(e);
            return false;
        }
    }

    private void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
    {
        Breadcrumbs.Clear();
        if (crumbs == null)
        {
            if (TryGetFirstFolder(out StorageFolder firstFolder))
                Breadcrumbs.Add(firstFolder);
        }
        else
        {
            foreach (StorageFolder storageFolder in crumbs)
            {
                Breadcrumbs.Add(storageFolder);
            }
        }
    }

    /// <summary>
    /// Requests adding a new folder to the Videos library.
    /// Throws on failure; the view layer handles the error notification.
    /// </summary>
    public async Task AddFolderAsync()
    {
        await _libraryContext.VideosLibrary?.RequestAddFolderAsync();
    }

    /// <summary>
    /// Sends an error notification message via the messenger.
    /// The view layer calls this with a localized title after an operation fails.
    /// </summary>
    /// <param name="title">The localized notification title.</param>
    /// <param name="message">The error detail message.</param>
    public void SendErrorMessage(string? title, string message)
    {
        Messenger.Send(new ErrorMessage(title, message));
    }
}

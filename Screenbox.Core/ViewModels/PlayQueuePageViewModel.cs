#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PlayQueuePageViewModel : ObservableRecipient
    {
        private readonly IFilesService _filesService;

        public PlayQueuePageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
        }

        [RelayCommand]
        private void AddUrl(Uri? uri)
        {
            if (uri == null) return;
            Messenger.Send(new PlayMediaMessage(uri));
        }

        /// <summary>
        /// Opens a folder picker and queues all supported media files in the selected folder.
        /// Throws on failure; the view layer handles the error notification via <see cref="Commands.NotificationCommand"/>.
        /// </summary>
        [RelayCommand]
        private async Task AddFolderAsync()
        {
            StorageFolder? folder = await _filesService.PickFolderAsync();
            if (folder == null) return;
            IReadOnlyList<IStorageItem> items = await _filesService.GetSupportedItems(folder).GetItemsAsync();
            Messenger.Send(new PlayFilesMessage(items));
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
}

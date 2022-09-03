#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Screenbox.ViewModels
{
    internal partial class FolderViewPageViewModel : ObservableRecipient
    {
        public event EventHandler<FolderViewNavigationEventArgs>? NavigationRequested;

        public ObservableCollection<StorageItemViewModel> Items { get; }

        public IReadOnlyList<StorageFolder> Breadcrumbs { get; private set; }

        [ObservableProperty] private bool _isEmpty;

        private readonly IFilesService _filesService;

        public FolderViewPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            Breadcrumbs = Array.Empty<StorageFolder>();
            Items = new ObservableCollection<StorageItemViewModel>();
        }

        public async Task OnNavigatedTo(object? parameter)
        {
            switch (parameter)
            {
                case IReadOnlyList<StorageFolder> { Count: > 0 } breadcrumbs:
                    Breadcrumbs = breadcrumbs;
                    await FetchFolderContentAsync(breadcrumbs.Last());
                    break;
                case StorageLibrary library:
                    await FetchFolderContentAsync(library);
                    break;
            }
        }

        public void VideosItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is StorageItemViewModel item)
            {
                if (item.Media != null)
                {
                    Messenger.Send(new PlayMediaMessage(item.Media));
                }
                else if (item.StorageItem is StorageFolder folder)
                {
                    StorageFolder[] crumbs = Breadcrumbs.Append(folder).ToArray();
                    NavigationRequested?.Invoke(this, new FolderViewNavigationEventArgs(crumbs));
                }
            }
        }

        private async Task FetchFolderContentAsync(StorageFolder folder)
        {
            // TODO: Virtualize data fetching
            Items.Clear();
            IReadOnlyCollection<StorageFolder> subfolders = await folder.GetFoldersAsync();
            foreach (StorageFolder subfolder in subfolders)
            {
                StorageItemViewModel item = new(subfolder);
                Items.Add(item);
            }

            IReadOnlyList<StorageFile> files = await _filesService.GetSupportedFilesAsync(folder);
            foreach (StorageFile file in files)
            {
                StorageItemViewModel item = new(file);
                Items.Add(item);
            }

            IsEmpty = Items.Count == 0;
            foreach (StorageItemViewModel item in Items)
            {
                await item.LoadFolderContentAsync();
                if (item.Media != null)
                {
                    await item.Media.LoadDetailsAsync();
                }
            }
        }

        private async Task FetchFolderContentAsync(StorageLibrary library)
        {
            if (library.Folders.Count <= 0)
            {
                IsEmpty = true;
                return;
            }

            if (library.Folders.Count == 1)
            {
                // StorageLibrary is always the root
                // Fetch content of the only folder if applicable
                StorageFolder folder = library.Folders[0];
                Breadcrumbs = new[] { folder };
                await FetchFolderContentAsync(folder);
            }
            else
            {
                Items.Clear();
                foreach (StorageFolder folder in library.Folders)
                {
                    StorageItemViewModel item = new(folder);
                    Items.Add(item);
                    await item.LoadFolderContentAsync();
                }

                IsEmpty = Items.Count == 0;
            }
        }
    }
}

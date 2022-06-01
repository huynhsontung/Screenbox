#nullable enable

using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Screenbox.ViewModels
{
    internal partial class FolderViewPageViewModel : ObservableRecipient
    {
        public event EventHandler<FolderViewNavigationEventArgs>? NavigationRequested;

        public ObservableCollection<StorageItemViewModel> Items { get; }

        public StorageFolder? Folder { get; private set; }

        [ObservableProperty] private IReadOnlyList<StorageFolder> _breadcrumbs;

        private readonly IFilesService _filesService;

        public FolderViewPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            _breadcrumbs = Array.Empty<StorageFolder>();
            Items = new ObservableCollection<StorageItemViewModel>();
        }

        public async Task OnNavigatedTo(object parameter)
        {
            Breadcrumbs = parameter as IReadOnlyList<StorageFolder> ?? Array.Empty<StorageFolder>();
            if (Breadcrumbs.Count > 0)
            {
                await FetchFolderContentAsync(Breadcrumbs.Last());
            }
            else
            {
                await LoadVideosFromLibraryAsync();
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
            Folder = folder;

            // TODO: Virtualize data fetching
            Items.Clear();
            IReadOnlyCollection<StorageFolder> subfolders = await folder.GetFoldersAsync();
            foreach (StorageFolder subfolder in subfolders)
            {
                Items.Add(new StorageItemViewModel(subfolder));
            }

            IReadOnlyList<StorageFile> files = await _filesService.GetSupportedFilesAsync(folder);
            foreach (StorageFile file in files)
            {
                Items.Add(new StorageItemViewModel(file));
            }
        }

        private async Task LoadVideosFromLibraryAsync()
        {
            var library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            if (library.Folders.Count <= 0) return;

            if (library.Folders.Count == 1)
            {
                StorageFolder folder = library.Folders[0];
                Breadcrumbs = new StorageFolder[] { folder };
                await FetchFolderContentAsync(folder);
            }
            else
            {
                foreach (StorageFolder folder in library.Folders)
                {
                    Items.Add(new StorageItemViewModel(folder));
                }
            }
        }
    }
}

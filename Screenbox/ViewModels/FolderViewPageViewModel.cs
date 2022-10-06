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
using Windows.Storage.Search;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Factories;

namespace Screenbox.ViewModels
{
    internal sealed partial class FolderViewPageViewModel : ObservableRecipient
    {
        public event EventHandler<FolderViewNavigationEventArgs>? NavigationRequested;

        public ObservableCollection<StorageItemViewModel> Items { get; }

        public IReadOnlyList<StorageFolder> Breadcrumbs { get; private set; }

        [ObservableProperty] private bool _isEmpty;

        private readonly IFilesService _filesService;
        private readonly StorageItemViewModelFactory _storageVmFactory;
        private bool _isActive;

        public FolderViewPageViewModel(IFilesService filesService, StorageItemViewModelFactory storageVmFactory)
        {
            _filesService = filesService;
            _storageVmFactory = storageVmFactory;
            Breadcrumbs = Array.Empty<StorageFolder>();
            Items = new ObservableCollection<StorageItemViewModel>();
        }

        public async Task FetchContentAsync(object? parameter)
        {
            _isActive = true;
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

        public void Clean()
        {
            _isActive = false;
            Items.Clear();
        }

        [RelayCommand]
        private void Click(StorageItemViewModel item)
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

        private async Task FetchFolderContentAsync(StorageFolder folder)
        {
            Items.Clear();

            StorageItemQueryResult itemQuery = _filesService.GetSupportedItems(folder);
            uint fetchIndex = 0;
            while (_isActive)
            {
                IReadOnlyList<IStorageItem> items = await itemQuery.GetItemsAsync(fetchIndex, 30);
                if (items.Count == 0) break;
                fetchIndex += (uint)items.Count;
                foreach (IStorageItem storageItem in items)
                {
                    StorageItemViewModel item = _storageVmFactory.GetTransient(storageItem);
                    Items.Add(item);
                }
            }

            IsEmpty = Items.Count == 0;
            if (!_isActive) return;
            IEnumerable<Task> loadingTasks = Items.Select(item =>
                item.Media != null && !string.IsNullOrEmpty(item.Path)
                    ? Task.WhenAll(item.UpdateCaptionAsync(), item.Media.LoadThumbnailAsync())
                    : item.UpdateCaptionAsync());

            await Task.WhenAll(loadingTasks);
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
                    StorageItemViewModel item = _storageVmFactory.GetTransient(folder);
                    Items.Add(item);
                    await item.UpdateCaptionAsync();
                }

                IsEmpty = Items.Count == 0;
            }
        }
    }
}

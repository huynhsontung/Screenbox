#nullable enable

using System;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Screenbox.Factories;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal sealed partial class StorageItemViewModel : ObservableObject
    {
        public string Name { get; }

        public string Path { get; }

        public DateTimeOffset DateCreated { get; }

        public IStorageItem StorageItem { get; }

        public MediaViewModel? Media { get; }

        public bool IsFile { get; }

        [ObservableProperty] private string? _captionText;

        private readonly IFilesService _filesService;

        public StorageItemViewModel(IFilesService filesService,
            MediaViewModelFactory mediaFactory,
            IStorageItem storageItem)
        {
            _filesService = filesService;
            StorageItem = storageItem;
            Name = storageItem.Name;
            Path = storageItem.Path;
            DateCreated = storageItem.DateCreated;

            if (storageItem is StorageFile file)
            {
                IsFile = true;
                Media = mediaFactory.GetSingleton(file);
                Media.PropertyChanged += MediaOnPropertyChanged;
            }
        }

        public async Task LoadFolderContentAsync()
        {
            if (StorageItem is not StorageFolder folder) return;
            CaptionText = Strings.Resources.ItemsCount(await _filesService.GetSupportedItemCountAsync(folder));
        }

        private void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MediaViewModel.Caption) && !string.IsNullOrEmpty(Media?.Caption))
            {
                CaptionText = Media?.Caption;
            }
        }
    }
}

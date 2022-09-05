#nullable enable

using System;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Screenbox.Converters;

namespace Screenbox.ViewModels
{
    internal partial class StorageItemViewModel : ObservableObject
    {
        public string Name { get; }

        public string Path { get; }

        public DateTimeOffset DateCreated { get; }

        public IStorageItem StorageItem { get; }

        public MediaViewModel? Media { get; }

        public bool IsFile { get; }

        [ObservableProperty] private string? _captionText;

        public StorageItemViewModel(IStorageItem storageItem)
        {
            StorageItem = storageItem;
            Name = storageItem.Name;
            Path = storageItem.Path;
            DateCreated = storageItem.DateCreated;

            if (storageItem is StorageFile file)
            {
                IsFile = true;
                Media = new MediaViewModel(file);
                Media.PropertyChanged += MediaOnPropertyChanged;
            }
        }

        public async Task LoadFolderContentAsync()
        {
            if (StorageItem is not StorageFolder folder) return;
            IReadOnlyList<IStorageItem>? items = await folder.GetItemsAsync();
            CaptionText = Strings.Resources.ItemsCount(items.Count);
        }

        private void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Media.MusicProperties) when !string.IsNullOrEmpty(Media?.MusicProperties?.Artist):
                    CaptionText = Media?.MusicProperties?.Artist;
                    break;
                case nameof(Media.VideoProperties) when Media?.VideoProperties != null:
                    CaptionText ??= HumanizedDurationConverter.Convert(Media.VideoProperties.Duration);
                    break;
            }
        }
    }
}

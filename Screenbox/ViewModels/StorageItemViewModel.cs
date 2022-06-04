#nullable enable

using System;
using Windows.Storage;
using Microsoft.Toolkit.Mvvm.ComponentModel;
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

        public string Glyph { get; protected set; }

        public DateTimeOffset DateCreated { get; }

        public IStorageItem StorageItem { get; }

        public MediaViewModel? Media { get; }

        public bool IsFile { get; }

        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private string? _captionText;
        [ObservableProperty] private IReadOnlyList<IStorageItem>? _folderItems;

        public StorageItemViewModel(IStorageItem storageItem)
        {
            StorageItem = storageItem;
            Name = storageItem.Name;
            Path = storageItem.Path;
            DateCreated = storageItem.DateCreated;
            Glyph = GetGlyph(storageItem);

            if (storageItem is IStorageFile file)
            {
                IsFile = true;
                Media = new MediaViewModel(file);
                Media.PropertyChanged += MediaOnPropertyChanged;
            }
        }

        public async Task LoadFolderContentAsync()
        {
            if (StorageItem is not StorageFolder folder || FolderItems != null) return;
            FolderItems = await folder.GetItemsAsync();
            CaptionText = Strings.Resources.ItemsCount(FolderItems.Count);
        }

        public static string GetGlyph(IStorageItem item)
        {
            string glyph = "\ue8b7";
            if (item is IStorageFile file)
            {
                glyph = "\ue8a5";
                if (file.ContentType.StartsWith("video"))
                {
                    glyph = "\ue8b2";
                }
                else if (file.ContentType.StartsWith("audio"))
                {
                    glyph = "\ue8d6";
                }
            }

            return glyph;
        }

        private void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Media.VideoProperties))
            {
                CaptionText = Media?.VideoProperties == null || Media.VideoProperties.Duration == default
                    ? null
                    : HumanizedDurationConverter.Convert(Media.VideoProperties.Duration);
            }
        }

    }
}

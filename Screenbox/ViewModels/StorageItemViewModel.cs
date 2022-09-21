#nullable enable

using System;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Converters;
using Screenbox.Services;

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

        private readonly IFilesService _filesService;

        public StorageItemViewModel(IStorageItem storageItem) :
            this(App.Services.GetRequiredService<IFilesService>(), storageItem)
        {
        }

        public StorageItemViewModel(IFilesService filesService, IStorageItem storageItem)
        {
            _filesService = filesService;
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
            CaptionText = Strings.Resources.ItemsCount(await _filesService.GetSupportedItemCountAsync(folder));
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

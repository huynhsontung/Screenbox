#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class StorageItemViewModel : ObservableObject
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
            DateCreated = storageItem.DateCreated;

            if (storageItem is StorageFile file)
            {
                IsFile = true;
                Media = mediaFactory.GetSingleton(file);
                Name = Media.Name;
                Path = Media.Location;
            }
            else
            {
                Name = storageItem.Name;
                Path = storageItem.Path;
            }
        }

        public async Task UpdateCaptionAsync()
        {
            try
            {
                switch (StorageItem)
                {
                    case StorageFolder folder when !string.IsNullOrEmpty(folder.Path):
                        uint itemCount = await _filesService.GetSupportedItemCountAsync(folder);
                        CaptionText = ResourceHelper.GetString(PluralResourceName.ItemsCount, itemCount);
                        break;
                    case StorageFile file:
                        if (!string.IsNullOrEmpty(Media?.Caption))
                        {
                            CaptionText = Media?.Caption;
                        }
                        else
                        {
                            string[] additionalPropertyKeys =
                            {
                                SystemProperties.Music.Artist,
                                SystemProperties.Media.Duration
                            };

                            IDictionary<string, object> additionalProperties =
                                await file.Properties.RetrievePropertiesAsync(additionalPropertyKeys);

                            if (additionalProperties[SystemProperties.Music.Artist] is string[] { Length: > 0 } contributingArtists)
                            {
                                CaptionText = string.Join(", ", contributingArtists);
                            }
                            else if (additionalProperties[SystemProperties.Media.Duration] is ulong ticks and > 0)
                            {
                                TimeSpan duration = TimeSpan.FromTicks((long)ticks);
                                CaptionText = Humanizer.ToDuration(duration);
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                LogService.Log(e);
            }
        }
    }
}

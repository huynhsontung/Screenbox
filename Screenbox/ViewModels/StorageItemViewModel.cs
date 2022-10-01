#nullable enable

using System;
using System.Collections.Generic;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using Screenbox.Converters;
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
            }
        }

        public async Task UpdateCaptionAsync()
        {
            try
            {
                switch (StorageItem)
                {
                    case StorageFolder folder:
                        CaptionText = Strings.Resources.ItemsCount(await _filesService.GetSupportedItemCountAsync(folder));
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
                                CaptionText = HumanizedDurationConverter.Convert(duration);
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

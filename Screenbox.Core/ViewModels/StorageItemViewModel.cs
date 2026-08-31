using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Factories;
using Screenbox.Core.Services;
using Windows.Storage;
using WinRT;

namespace Screenbox.Core.ViewModels;

public sealed partial class StorageItemViewModel : ObservableObject
{
    public string Name { get; }

    public string Path { get; }

    public DateTimeOffset DateCreated { get; }

    public IStorageItem StorageItem { get; }

    public MediaViewModel? Media { get; }

    public bool IsFile { get; }

    [ObservableProperty] public partial string CaptionText { get; set; }
    [ObservableProperty] public partial uint ItemCount { get; set; }

    private readonly IFilesService _filesService;
    private readonly ILogger<StorageItemViewModel> _logger;

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public StorageItemViewModel(IFilesService filesService,
        MediaViewModelFactory mediaFactory,
        ILogger<StorageItemViewModel> logger,
        IStorageItem storageItem)
    {
        _filesService = filesService;
        _logger = logger;
        StorageItem = storageItem;
        CaptionText = string.Empty;
        DateCreated = storageItem.DateCreated;

        if (storageItem is StorageFile file)
        {
            IsFile = true;
            Media = mediaFactory.GetOrCreate(file);
            Name = Media.Name;
            Path = Media.Location;
        }
        else
        {
            Name = storageItem.Name;
            Path = storageItem.Path;
        }
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async Task UpdateCaptionAsync()
    {
        try
        {
            switch (StorageItem)
            {
                case StorageFolder folder when !string.IsNullOrEmpty(folder.Path):
                    ItemCount = await _filesService.GetSupportedItemCountAsync(folder);
                    break;
                case StorageFile file:
                    if (!string.IsNullOrEmpty(Media?.Caption))
                    {
                        CaptionText = Media?.Caption ?? string.Empty;
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
            _logger.LogError(e, "Failed to update the caption for storage item '{Path}'.", Path);
        }
    }
}

using Screenbox.Core.Services;
using Windows.Storage;
using Microsoft.Extensions.Logging;
using StorageItemViewModel = Screenbox.Core.ViewModels.StorageItemViewModel;

namespace Screenbox.Core.Factories;

public sealed class StorageItemViewModelFactory
{
    private readonly IFilesService _filesService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly ILogger<StorageItemViewModel> _logger;

    public StorageItemViewModelFactory(
        IFilesService filesService,
        MediaViewModelFactory mediaFactory,
        ILogger<StorageItemViewModel> logger)
    {
        _filesService = filesService;
        _mediaFactory = mediaFactory;
        _logger = logger;
    }

    public StorageItemViewModel GetInstance(IStorageItem storageItem)
    {
        return new StorageItemViewModel(_filesService, _mediaFactory, _logger, storageItem);
    }
}

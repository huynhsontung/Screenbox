using Windows.Storage;
using Screenbox.Core.Services;
using Screenbox.ViewModels;

namespace Screenbox.Core.Factories
{
    public sealed class StorageItemViewModelFactory
    {
        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;

        public StorageItemViewModelFactory(IFilesService filesService, MediaViewModelFactory mediaFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
        }

        public StorageItemViewModel GetInstance(IStorageItem storageItem)
        {
            return new StorageItemViewModel(_filesService, _mediaFactory, storageItem);
        }
    }
}

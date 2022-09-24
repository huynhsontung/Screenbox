using Windows.Storage;
using Screenbox.Services;
using Screenbox.ViewModels;

namespace Screenbox.Factories
{
    internal sealed class StorageItemViewModelFactory
    {
        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;

        public StorageItemViewModelFactory(IFilesService filesService, MediaViewModelFactory mediaFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
        }

        public StorageItemViewModel GetTransient(IStorageItem storageItem)
        {
            return new StorageItemViewModel(_filesService, _mediaFactory, storageItem);
        }
    }
}

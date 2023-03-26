using Windows.Storage;
using Screenbox.Core.Services;

using StorageItemViewModel = Screenbox.Core.ViewModels.StorageItemViewModel;

namespace Screenbox.Core.Factories
{
    public sealed class StorageItemViewModelFactory
    {
        private readonly IFilesService _filesService;
        private readonly IResourceService _resourceService;
        private readonly MediaViewModelFactory _mediaFactory;

        public StorageItemViewModelFactory(IFilesService filesService, IResourceService resourceService, MediaViewModelFactory mediaFactory)
        {
            _filesService = filesService;
            _resourceService = resourceService;
            _mediaFactory = mediaFactory;
        }

        public StorageItemViewModel GetInstance(IStorageItem storageItem)
        {
            return new StorageItemViewModel(_filesService, _resourceService, _mediaFactory, storageItem);
        }
    }
}

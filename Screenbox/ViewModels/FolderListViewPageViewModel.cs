using Screenbox.Factories;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    // To support navigation type matching
    internal sealed class FolderListViewPageViewModel : FolderViewPageViewModel
    {
        public FolderListViewPageViewModel(IFilesService filesService,
            INavigationService navigationService,
            StorageItemViewModelFactory storageVmFactory):
            base(filesService, navigationService, storageVmFactory)
        {}
    }
}

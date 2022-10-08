#nullable enable

using Screenbox.Factories;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    // To support navigation type matching
    internal sealed class FolderListViewPageViewModel : FolderViewPageViewModel
    {
        private readonly INavigationService _navigationService;

        public FolderListViewPageViewModel(IFilesService filesService,
            INavigationService navigationService,
            StorageItemViewModelFactory storageVmFactory) :
            base(filesService, navigationService, storageVmFactory)
        {
            _navigationService = navigationService;
        }

        protected override void Navigate(object? parameter = null)
        {
            _navigationService.NavigateExisting(typeof(FolderListViewPageViewModel), parameter);
        }
    }
}

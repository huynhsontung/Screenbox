#nullable enable

using Screenbox.Core.Factories;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels
{
    // To support navigation type matching
    public sealed class FolderListViewPageViewModel : FolderViewPageViewModel
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
            _navigationService.NavigateExisting(typeof(FolderListViewPageViewModel),
                new NavigationMetadata(NavData?.RootViewModelType ?? typeof(FolderListViewPageViewModel), parameter));
        }
    }
}

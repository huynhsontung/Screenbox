#nullable enable

using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Screenbox.Core.ViewModels
{
    public class FolderViewWithHeaderPageViewModel
    {
        public string TitleText => Breadcrumbs.LastOrDefault()?.Name ?? string.Empty;

        public IReadOnlyList<StorageFolder> Breadcrumbs { get; private set; }

        private readonly INavigationService _navigationService;
        private NavigationMetadata? _navData;

        public FolderViewWithHeaderPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            Breadcrumbs = Array.Empty<StorageFolder>();
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is NavigationMetadata { Parameter: IReadOnlyList<StorageFolder> breadcrumbs } source)
            {
                _navData = source;
                Breadcrumbs = breadcrumbs;
            }
        }

        public void OnBreadcrumbBarItemClicked(int index)
        {
            IReadOnlyList<StorageFolder> crumbs = Breadcrumbs.Take(index + 1).ToArray();
            if (_navData != null)
            {
                if (index == 0)
                {
                    _navigationService.Navigate(_navData.RootViewModelType);
                }
                else
                {
                    _navigationService.Navigate(typeof(FolderViewWithHeaderPageViewModel),
                        new NavigationMetadata(_navData.RootViewModelType, crumbs));
                }
            }
            else
            {
                _navigationService.Navigate(typeof(FolderViewWithHeaderPageViewModel),
                    new NavigationMetadata(typeof(FolderViewWithHeaderPageViewModel), crumbs));
            }
        }
    }
}

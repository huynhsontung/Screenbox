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
        private NavigationMetadata? _source;

        public FolderViewWithHeaderPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            Breadcrumbs = Array.Empty<StorageFolder>();
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is NavigationMetadata { Parameter: IReadOnlyList<StorageFolder> breadcrumbs } source)
            {
                _source = source;
                Breadcrumbs = breadcrumbs;
            }
        }

        public void OnBreadcrumbBarItemClicked(int index)
        {
            IReadOnlyList<StorageFolder> crumbs = Breadcrumbs.Take(index + 1).ToArray();
            _navigationService.Navigate(typeof(FolderViewWithHeaderPageViewModel),
                new NavigationMetadata(_source?.RootPageType ?? typeof(FolderViewWithHeaderPageViewModel), crumbs));
        }
    }
}

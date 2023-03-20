#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels
{
    public class FolderViewWithHeaderPageViewModel
    {
        public string TitleText => Breadcrumbs.LastOrDefault()?.Name ?? string.Empty;

        public IReadOnlyList<StorageFolder> Breadcrumbs { get; private set; }

        private readonly INavigationService _navigationService;

        public FolderViewWithHeaderPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            Breadcrumbs = Array.Empty<StorageFolder>();
        }

        public void OnNavigatedTo(object? parameter)
        {
            if (parameter is IReadOnlyList<StorageFolder> breadcrumbs)
            {
                Breadcrumbs = breadcrumbs;
            }
        }

        public void OnBreadcrumbBarItemClicked(int index)
        {
            IReadOnlyList<StorageFolder> crumbs = Breadcrumbs.Take(index + 1).ToArray();
            _navigationService.Navigate(typeof(FolderViewWithHeaderPageViewModel), crumbs);
        }
    }
}

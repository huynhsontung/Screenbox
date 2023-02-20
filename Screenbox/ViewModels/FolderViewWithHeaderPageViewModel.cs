using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Services;
using Windows.Storage;

namespace Screenbox.ViewModels
{
    internal class FolderViewWithHeaderPageViewModel
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

        public void OnBreadcrumbBarItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            IReadOnlyList<StorageFolder> crumbs = Breadcrumbs.Take(args.Index + 1).ToArray();
            _navigationService.Navigate(typeof(FolderViewWithHeaderPageViewModel), crumbs);
        }
    }
}

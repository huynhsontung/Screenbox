#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NetworkPage : Page, IContentFrame
    {
        public Type SourcePageType => FolderViewFrame.SourcePageType;

        public object? FrameContent => FolderViewFrame.Content;

        public bool CanGoBack => FolderViewFrame.CanGoBack;

        internal NetworkPageViewModel ViewModel => (NetworkPageViewModel)DataContext;

        public NetworkPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<NetworkPageViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FolderViewFrame.Navigate(typeof(FolderListViewPage), new[] { KnownFolders.MediaServerDevices },
                new SuppressNavigationTransitionInfo());
        }

        public void GoBack()
        {
            FolderViewFrame.GoBack(new SuppressNavigationTransitionInfo());
        }

        public void Navigate(Type pageType, object? parameter)
        {
            FolderViewFrame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
        }

        private void BreadcrumbBar_OnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            if (FolderViewFrame.Content is not FolderListViewPage view) return;
            IReadOnlyList<StorageFolder> crumbs = view.ViewModel.Breadcrumbs.Take(args.Index + 1).ToArray();
            FolderViewFrame.Navigate(typeof(FolderListViewPage), crumbs, new SuppressNavigationTransitionInfo());
        }

        private void FolderViewFrame_OnNavigated(object sender, NavigationEventArgs e)
        {
            IReadOnlyList<StorageFolder>? crumbs = e.Parameter as IReadOnlyList<StorageFolder>;
            ViewModel.UpdateBreadcrumbs(crumbs);
            if (crumbs?.Count == 1)
            {
                FolderListViewPage page = (FolderListViewPage)e.Content;
                page.ViewModel.PropertyChanged -= FolderViewModel_PropertyChanged;
                page.ViewModel.PropertyChanged += FolderViewModel_PropertyChanged;
            }
        }

        private void FolderViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FolderViewPageViewModel vm = (FolderViewPageViewModel)sender;
            switch (e.PropertyName)
            {
                case nameof(FolderViewPageViewModel.IsEmpty):
                    VisualStateManager.GoToState(this, vm.IsEmpty ? "NoNetworkDrive" : "FolderView", true);
                    break;
            }
        }
    }
}

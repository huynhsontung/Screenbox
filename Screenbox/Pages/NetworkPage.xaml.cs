#nullable enable

using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
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
    public sealed partial class NetworkPage : ContentPage
    {
        internal NetworkPageViewModel ViewModel => (NetworkPageViewModel)DataContext;

        public NetworkPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<NetworkPageViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FolderViewFrame.Navigate(typeof(FolderViewPage), new[] { KnownFolders.MediaServerDevices },
                new SuppressNavigationTransitionInfo());
        }

        public override void GoBack()
        {
            FolderViewFrame.GoBack(new SuppressNavigationTransitionInfo());
        }

        private void BreadcrumbBar_OnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            if (FolderViewFrame.Content is not FolderViewPage view) return;
            IReadOnlyList<StorageFolder> crumbs = view.ViewModel.Breadcrumbs.Take(args.Index + 1).ToArray();
            FolderViewFrame.Navigate(typeof(FolderViewPage), crumbs, new SuppressNavigationTransitionInfo());
        }

        private void FolderViewFrame_OnNavigated(object sender, NavigationEventArgs e)
        {
            IReadOnlyList<StorageFolder>? crumbs = e.Parameter as IReadOnlyList<StorageFolder>;
            ViewModel.UpdateBreadcrumbs(crumbs);
        }
    }
}

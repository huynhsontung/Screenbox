using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
        internal VideosPageViewModel ViewModel => (VideosPageViewModel)DataContext;

        public NetworkPage()
        {
            DataContext = App.Services.GetRequiredService<VideosPageViewModel>();
            this.InitializeComponent();
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
    }
}

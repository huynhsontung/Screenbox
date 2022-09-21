#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;
using Windows.Storage;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideosPage : ContentPage
    {
        internal VideosPageViewModel ViewModel => (VideosPageViewModel)DataContext;

        public VideosPage()
        {
            DataContext = App.Services.GetRequiredService<VideosPageViewModel>();
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FolderViewFrame.Navigate(typeof(FolderViewPage), new[] { KnownFolders.VideosLibrary },
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

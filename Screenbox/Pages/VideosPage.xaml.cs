#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideosPage : Page, IContentFrame
    {
        public Type ContentSourcePageType => FolderViewFrame.SourcePageType;

        public object? FrameContent => FolderViewFrame.Content;

        public bool CanGoBack => FolderViewFrame.CanGoBack;

        internal VideosPageViewModel ViewModel => (VideosPageViewModel)DataContext;

        public VideosPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<VideosPageViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FolderViewFrame.Navigate(typeof(FolderViewPage), new[] { KnownFolders.VideosLibrary },
                new SuppressNavigationTransitionInfo());
        }

        public void GoBack()
        {
            FolderViewFrame.GoBack(new SuppressNavigationTransitionInfo());
        }

        public void NavigateContent(Type pageType, object? parameter)
        {
            FolderViewFrame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
        }
    }
}

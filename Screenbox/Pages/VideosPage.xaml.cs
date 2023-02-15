#nullable enable

using System;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

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

        internal CommonViewModel Common { get; }

        public VideosPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<VideosPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.OnNavigatedTo();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (FolderViewFrame.Content is FolderViewPage page)
            {
                page.ViewModel.Clean();
            }
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

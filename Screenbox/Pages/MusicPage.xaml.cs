#nullable enable

using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicPage : Page, IContentFrame
    {
        public object? FrameContent => ContentFrame;
        public Type ContentSourcePageType => ContentFrame.SourcePageType;
        public bool CanGoBack => ContentFrame.CanGoBack;
        internal MusicPageViewModel ViewModel => (MusicPageViewModel)DataContext;

        public MusicPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<MusicPageViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ContentFrame.Navigate(typeof(SongsPage), null, new SuppressNavigationTransitionInfo());
            LibraryNavigationView.SelectedItem = LibraryNavigationView.MenuItems[0];
        }

        public void GoBack()
        {
            ContentFrame.GoBack();
        }

        public void NavigateContent(Type pageType, object? parameter)
        {
            ContentFrame.Navigate(pageType, parameter);
        }
    }
}

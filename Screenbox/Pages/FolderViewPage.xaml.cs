using Microsoft.Extensions.DependencyInjection;
using Screenbox.Core;
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
    public sealed partial class FolderViewPage : Page
    {
        internal FolderViewPageViewModel ViewModel => (FolderViewPageViewModel)DataContext;

        public FolderViewPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<FolderViewPageViewModel>();
            ViewModel.NavigationRequested += ViewModel_NavigationRequested;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.OnNavigatedTo(e.Parameter);
        }

        private void ViewModel_NavigationRequested(object sender, FolderViewNavigationEventArgs e)
        {
            Frame.Navigate(typeof(FolderViewPage), e.Breadcrumbs, new SuppressNavigationTransitionInfo());
        }
    }
}

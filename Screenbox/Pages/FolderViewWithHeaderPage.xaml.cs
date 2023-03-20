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
    public sealed partial class FolderViewWithHeaderPage : Page
    {
        internal FolderViewWithHeaderPageViewModel ViewModel => (FolderViewWithHeaderPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public FolderViewWithHeaderPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<FolderViewWithHeaderPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.OnNavigatedTo(e.Parameter);
            ContentFrame.Navigate(typeof(FolderViewPage), e.Parameter, new SuppressNavigationTransitionInfo());
        }

        private void BreadcrumbBar_OnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            ViewModel.OnBreadcrumbBarItemClicked(args.Index);
        }
    }
}

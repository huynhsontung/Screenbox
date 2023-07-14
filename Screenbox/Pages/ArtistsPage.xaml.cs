using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArtistsPage : Page
    {
        internal ArtistsPageViewModel ViewModel => (ArtistsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private bool _navigatedBack;

        public ArtistsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<ArtistsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.FetchArtists();
            _navigatedBack = e.NavigationMode == NavigationMode.Back;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.OnNavigatedFrom();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Common.SaveScrollingState(ArtistGridView, this);
        }

        private void ArtistGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_navigatedBack)
            {
                Common.TryRestoreScrollingStateOnce(ArtistGridView, this);
            }
        }

        private void ArtistGridView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            // TODO: Load artist image from the internet
        }
    }
}

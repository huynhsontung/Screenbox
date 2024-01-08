using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
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
    public sealed partial class SongsPage : Page
    {
        internal SongsPageViewModel ViewModel => (SongsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private double _contentVerticalOffset;

        public SongsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SongsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.FetchSongs();
            if (e.NavigationMode == NavigationMode.Back &&
                Common.TryGetScrollingState(nameof(SongsPage), Frame.BackStackDepth, out double verticalOffset))
            {
                _contentVerticalOffset = verticalOffset;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.OnNavigatedFrom();
        }

        private void SongListView_OnLoaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer? scrollViewer = SongListView.FindDescendant<ScrollViewer>();
            if (scrollViewer == null) return;
            scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
            if (_contentVerticalOffset > 0)
            {
                scrollViewer.ChangeView(null, _contentVerticalOffset, null, true);
            }
        }

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            Common.SaveScrollingState(e.NextView.VerticalOffset, nameof(SongsPage), Frame.BackStackDepth);
        }
    }
}

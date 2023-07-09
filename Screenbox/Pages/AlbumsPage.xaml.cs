using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
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
    public sealed partial class AlbumsPage : Page
    {
        internal AlbumsPageViewModel ViewModel => (AlbumsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private bool _navigatedBack;

        public AlbumsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AlbumsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.FetchAlbums();
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
            if (AlbumGridView.FindDescendant<ScrollViewer>() is { } scrollViewer)
                Common.ScrollingStates[nameof(AlbumsPage) + Frame.BackStackDepth] = scrollViewer.VerticalOffset;
        }

        private void AlbumGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_navigatedBack && Common.ScrollingStates.TryGetValue(nameof(AlbumsPage) + Frame.BackStackDepth, out double verticalOffset))
            {
                AlbumGridView.FindDescendant<ScrollViewer>()?.ChangeView(null, verticalOffset, null, true);
            }
        }

        private void AlbumGridView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;
            if (args.Item is AlbumViewModel album)
            {
                album.RelatedSongs[0].LoadThumbnailAsync();
            }
        }
    }
}

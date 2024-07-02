using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
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

        private double _contentVerticalOffset;

        public AlbumsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AlbumsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SongsPageViewModel.SortBy))
            {
                var state = ViewModel.SortBy switch
                {
                    "artist" => "SortByArtist",
                    _ => "SortByTitle"
                };
                VisualStateManager.GoToState(this, state, true);
                UpdateGroupViewItemWidth();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.FetchAlbums();
            if (e.NavigationMode == NavigationMode.Back &&
                Common.TryGetScrollingState(nameof(AlbumsPage), Frame.BackStackDepth, out double verticalOffset))
            {
                _contentVerticalOffset = verticalOffset;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.OnNavigatedFrom();
        }

        private void AlbumGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer? scrollViewer = AlbumGridView.FindDescendant<ScrollViewer>();
            if (scrollViewer == null) return;
            scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
            if (_contentVerticalOffset > 0)
            {
                scrollViewer.ChangeView(null, _contentVerticalOffset, null, true);
            }
        }

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            Common.SaveScrollingState(e.NextView.VerticalOffset, nameof(AlbumsPage), Frame.BackStackDepth);
        }

        private string GetSortByText(string tag)
        {
            var item = SortByFlyout.Items?.FirstOrDefault(x => x.Tag as string == tag) ?? SortByFlyout.Items?.FirstOrDefault();
            return (item as MenuFlyoutItem)?.Text ?? string.Empty;
        }

        private void GroupOverview_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGroupViewItemWidth();
        }

        private void UpdateGroupViewItemWidth()
        {
            if (GroupOverview.ItemsPanelRoot == null) return;
            var gridContentWidth = GroupOverview.ActualWidth -
                                   (GroupOverview.Margin.Left + GroupOverview.Margin.Right) -
                                   (GroupOverview.Padding.Left + GroupOverview.Padding.Right);
            var numColumns = (int)gridContentWidth / 400;
            var itemWidth = numColumns > 0 ? gridContentWidth / numColumns : gridContentWidth;
            itemWidth -= 4; // Item paddings
            itemWidth = Math.Floor(itemWidth);

            foreach (var child in GroupOverview.ItemsPanelRoot.Children)
            {
                var element = (FrameworkElement)child;
                element.Width = ViewModel.SortBy == "year"
                    ? 80
                    : GroupOverview.HorizontalAlignment != HorizontalAlignment.Stretch
                        ? double.NaN
                        : itemWidth;
            }
        }
    }
}

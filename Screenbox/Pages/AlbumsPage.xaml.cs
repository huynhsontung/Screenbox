using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Core.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.System;
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

        private readonly DispatcherQueue _dispatcherQueue;

        private double _contentVerticalOffset;

        public AlbumsPage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
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
                SavePageState(0);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back
                && Common.TryGetPageState(nameof(AlbumsPage), Frame.BackStackDepth, out var state)
                && state is KeyValuePair<string, double> pair)
            {
                ViewModel.SortBy = pair.Key;
                _contentVerticalOffset = pair.Value;
            }

            if (!_dispatcherQueue.TryEnqueue(ViewModel.FetchAlbums))
                ViewModel.FetchAlbums();
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
            SavePageState(e.NextView.VerticalOffset);
        }

        private void SavePageState(double verticalOffset)
        {
            Common.SavePageState(new KeyValuePair<string, double>(ViewModel.SortBy, verticalOffset), nameof(AlbumsPage),
                Frame.BackStackDepth);
        }

        private string GetSortByText(string tag)
        {
            var item = SortByFlyout.Items?.FirstOrDefault(x => x.Tag as string == tag) ?? SortByFlyout.Items?.FirstOrDefault();
            return (item as MenuFlyoutItem)?.Text ?? string.Empty;
        }
    }
}

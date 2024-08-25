﻿using CommunityToolkit.Mvvm.DependencyInjection;
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
    public sealed partial class ArtistsPage : Page
    {
        internal ArtistsPageViewModel ViewModel => (ArtistsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private double _contentVerticalOffset;

        public ArtistsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<ArtistsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back
                && Common.TryGetPageState(nameof(ArtistsPage), Frame.BackStackDepth, out var state)
                && state is double verticalOffset)
            {
                _contentVerticalOffset = verticalOffset;
            }

            ViewModel.FetchArtists();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.OnNavigatedFrom();
        }

        private void ArtistGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer? scrollViewer = ArtistGridView.FindDescendant<ScrollViewer>();
            if (scrollViewer == null) return;
            scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
            if (_contentVerticalOffset > 0)
            {
                scrollViewer.ChangeView(null, _contentVerticalOffset, null, true);
            }
        }

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            Common.SavePageState(e.NextView.VerticalOffset, nameof(ArtistsPage), Frame.BackStackDepth);
        }

        private void ArtistGridView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            // TODO: Load artist image from the internet
        }
    }
}

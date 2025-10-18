using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
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
    public sealed partial class SongsPage : Page
    {
        internal SongsPageViewModel ViewModel => (SongsPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private double _contentVerticalOffset;

        private readonly DispatcherQueue _dispatcherQueue;

        public SongsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SongsPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SongsPageViewModel.SortBy))
            {
                var state = ViewModel.SortBy switch
                {
                    "album" => "SortByAlbum",
                    "artist" => "SortByArtist",
                    _ => "SortByTitle"
                };
                VisualStateManager.GoToState(this, state, true);
                UpdateSortByFlyout();
                SavePageState(0);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back
                && Common.TryGetPageState(nameof(SongsPage), Frame.BackStackDepth, out var state)
                && state is KeyValuePair<string, double> pair)
            {
                ViewModel.SortBy = pair.Key;
                _contentVerticalOffset = pair.Value;
            }

            if (!_dispatcherQueue.TryEnqueue(ViewModel.FetchSongs))
                ViewModel.FetchSongs();

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.OnNavigatedFrom();
            ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
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
            SavePageState(e.NextView.VerticalOffset);
        }

        private void SavePageState(double verticalOffset)
        {
            Common.SavePageState(new KeyValuePair<string, double>(ViewModel.SortBy, verticalOffset), nameof(SongsPage),
                Frame.BackStackDepth);
        }

        private string GetSortByText(string tag)
        {
            var item = SortByFlyout.Items?.FirstOrDefault(x => x.Tag as string == tag) ?? SortByFlyout.Items?.FirstOrDefault();
            return (item as MenuFlyoutItem)?.Text ?? string.Empty;
        }

        private string GetSortByButtonAutomationName(string value)
        {
            var optionText = GetSortByText(value);
            return Strings.Resources.SortByAutomationName(optionText);
        }

        private void UpdateSortByFlyout()
        {
            if ((SortByFlyout.Items?.FirstOrDefault(x => x.Tag as string == ViewModel.SortBy) ??
                 SortByFlyout.Items?.FirstOrDefault()) is RadioMenuFlyoutItem radioItem)
            {
                radioItem.IsChecked = true;
            }
        }
    }
}

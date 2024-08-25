#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Controls.Interactions;
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
    public sealed partial class FolderListViewPage : Page
    {
        internal FolderListViewPageViewModel ViewModel => (FolderListViewPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private double _contentVerticalOffset;
        private ScrollViewer? _scrollViewer;

        public FolderListViewPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<FolderListViewPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            ListView.ChoosingItemContainer += FolderViewOnChoosingItemContainer;
        }

        private void FolderViewOnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            ListView.ChoosingItemContainer -= FolderViewOnChoosingItemContainer;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back
                && Common.TryGetPageState(nameof(FolderListViewPage), Frame.BackStackDepth, out var state)
                && state is double verticalOffset)
            {
                _contentVerticalOffset = verticalOffset;
            }

            await ViewModel.OnNavigatedTo(e.Parameter);
            RestoreScrollVerticalOffset();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.OnNavigatedFrom();
        }

        private void RestoreScrollVerticalOffset()
        {
            if (_scrollViewer == null) return;
            if (_contentVerticalOffset > 0 && _scrollViewer.VerticalOffset == 0)
            {
                _scrollViewer.ChangeView(null, _contentVerticalOffset, null, true);
            }
        }

        private void FolderView_OnItemContextRequested(ListViewContextTriggerBehavior sender, ListViewContextRequestedEventArgs e)
        {
            if (e.Item.Content is not StorageItemViewModel content || content.Media == null)
            {
                e.Handled = true;
            }
        }

        private void ListView_OnLoaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = ListView.FindDescendant<ScrollViewer>();
            if (_scrollViewer == null) return;
            _scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
        }

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            Common.SavePageState(e.NextView.VerticalOffset, nameof(FolderListViewPage), Frame.BackStackDepth);
        }
    }
}

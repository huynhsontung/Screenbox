#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Behaviors;
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
    public sealed partial class FolderViewPage : Page
    {
        internal FolderViewPageViewModel ViewModel => (FolderViewPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public Visibility HeaderVisibility { get; private set; }

        private double _contentVerticalOffset;
        private ScrollViewer? _scrollViewer;

        public FolderViewPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<FolderViewPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            FolderView.ChoosingItemContainer += FolderViewOnChoosingItemContainer;
        }

        private void FolderViewOnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            FolderView.ChoosingItemContainer -= FolderViewOnChoosingItemContainer;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HeaderVisibility = e.Parameter is "VideosLibrary" ? Visibility.Collapsed : Visibility.Visible;
            TitleText.Visibility = HeaderVisibility;
            BreadcrumbBar.Visibility = HeaderVisibility;
            if (e.NavigationMode == NavigationMode.Back
                && Common.TryGetPageState(nameof(FolderViewPage), Frame.BackStackDepth, out var state)
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

        private static string GetAutomationName(bool isFile, string name, string fileInfo, uint itemsCount)
        {
            return isFile
                ? $"{Strings.Resources.File}; {name}, {fileInfo}"
                : $"{Strings.Resources.Folder}, {name}; {Strings.Resources.ItemsCount(itemsCount)}";
        }

        private static string GetCaptionText(bool isFile, string fileInfo, uint itemCount) =>
            isFile ? fileInfo : Strings.Resources.ItemsCount(itemCount);

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

        private void BreadcrumbBar_OnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            ViewModel.OnBreadcrumbBarItemClicked(args.Index);
        }

        private void FolderView_OnLoaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FolderView.FindDescendant<ScrollViewer>();
            if (_scrollViewer == null) return;
            _scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
        }

        private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            Common.SavePageState(e.NextView.VerticalOffset, nameof(FolderViewPage), Frame.BackStackDepth);
        }
    }
}

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Controls.Interactions;
using Screenbox.Core.ViewModels;
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
    public sealed partial class FolderViewPage : Page
    {
        internal FolderViewPageViewModel ViewModel => (FolderViewPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public Visibility HeaderVisibility { get; private set; }

        private readonly DispatcherQueue _dispatcherQueue;
        private bool _navigatedBack;

        public FolderViewPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<FolderViewPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            FolderView.ChoosingItemContainer += FolderViewOnChoosingItemContainer;
        }

        private void FolderViewOnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            FolderView.ChoosingItemContainer -= FolderViewOnChoosingItemContainer;
            if (_navigatedBack)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Common.TryRestoreScrollingStateOnce(FolderView, this);
                });
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _navigatedBack = e.NavigationMode == NavigationMode.Back;
            HeaderVisibility = e.Parameter is "VideosLibrary" ? Visibility.Collapsed : Visibility.Visible;
            TitleText.Visibility = HeaderVisibility;
            BreadcrumbBar.Visibility = HeaderVisibility;
            await ViewModel.OnNavigatedTo(e.Parameter);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Common.SaveScrollingState(FolderView, this);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.Clean();
        }

        private static string GetCaptionText(bool isFile, string fileInfo, uint itemCount) =>
            isFile ? fileInfo : Strings.Resources.ItemsCount(itemCount);

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
    }
}

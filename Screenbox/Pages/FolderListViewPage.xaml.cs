using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Controls.Interactions;
using Screenbox.Core.ViewModels;
using Windows.System;
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

        private readonly DispatcherQueue _dispatcherQueue;
        private bool _navigatedBack;

        public FolderListViewPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<FolderListViewPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            ListView.ChoosingItemContainer += FolderViewOnChoosingItemContainer;
        }

        private void FolderViewOnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            ListView.ChoosingItemContainer -= FolderViewOnChoosingItemContainer;
            if (_navigatedBack)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Common.TryRestoreScrollingStateOnce(ListView, this);
                });
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _navigatedBack = e.NavigationMode == NavigationMode.Back;
            await ViewModel.OnNavigatedTo(e.Parameter);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Common.SaveScrollingState(ListView, this);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.Clean();
        }

        private void FolderView_OnItemContextRequested(ListViewContextTriggerBehavior sender, ListViewContextRequestedEventArgs e)
        {
            if (e.Item.Content is not StorageItemViewModel content || content.Media == null)
            {
                e.Handled = true;
            }
        }
    }
}

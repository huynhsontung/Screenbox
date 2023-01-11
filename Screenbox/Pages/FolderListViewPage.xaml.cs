using Windows.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
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

        public FolderListViewPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<FolderListViewPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.FetchContentAsync(e.Parameter);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.Clean();
        }

        private void ListView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ItemFlyout.Items == null) return;
            if (e.OriginalSource is FrameworkElement { DataContext: StorageItemViewModel media } element)
            {
                ViewModel.ContextRequested = media;
                foreach (MenuFlyoutItemBase itemBase in ItemFlyout.Items)
                {
                    itemBase.DataContext = media;
                }

                ItemFlyout.ShowAt(element, e.GetPosition(element));
                e.Handled = true;
            }
        }

        private void ListView_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            if (ItemFlyout.Items == null) return;
            if (args.OriginalSource is SelectorItem { Content: StorageItemViewModel content } item)
            {
                ViewModel.ContextRequested = content;
                foreach (MenuFlyoutItemBase itemBase in ItemFlyout.Items)
                {
                    itemBase.DataContext = item.Content;
                }

                ItemFlyout.ShowAt(item);
                args.Handled = true;
            }
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Screenbox.Controls.Interactions;

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

        public FolderViewPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<FolderViewPageViewModel>();
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

        private async void FolderView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;
            if (args.Item != null) await ViewModel.LoadItemDetailsAsync((StorageItemViewModel)args.Item);
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

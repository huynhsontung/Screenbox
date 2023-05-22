using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Controls.Interactions;
using Screenbox.Core.ViewModels;

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
            DataContext = Ioc.Default.GetRequiredService<FolderListViewPageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
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

        private void FolderView_OnItemContextRequested(ListViewContextTriggerBehavior sender, ListViewContextRequestedEventArgs e)
        {
            if (e.Item.Content is not StorageItemViewModel content || content.Media == null)
            {
                e.Handled = true;
            }
        }
    }
}

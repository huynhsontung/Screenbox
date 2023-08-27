#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Controls;
using Screenbox.Controls.Interactions;
using Screenbox.Core;
using Screenbox.Core.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        internal HomePageViewModel ViewModel => (HomePageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private SelectorItem? _contextRequestedItem;

        public HomePage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<HomePageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            VisualStateManager.GoToState(this, ViewModel.HasRecentMedia ? "RecentMedia" : "Welcome", false);
        }

        private async void OpenUrlMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Uri? uri = await OpenUrlDialog.GetUrlAsync();
            if (uri != null)
            {
                ViewModel.OpenUrl(uri);
            }
        }

        private async void SetOptionsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_contextRequestedItem == null) return;
            MediaViewModelWithMruToken? item = (MediaViewModelWithMruToken)RecentFilesGridView.ItemFromContainer(_contextRequestedItem);
            SetOptionsDialog dialog = new(string.Join(' ', item.Media.Options));
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None) return;
            item.Media.SetOptions(dialog.Options);
            if (result == ContentDialogResult.Secondary)
            {
                ViewModel.PlayCommand.Execute(item);
            }
        }

        private void ListViewContextTriggerBehavior_OnContextRequested(ListViewContextTriggerBehavior sender, ListViewContextRequestedEventArgs args)
        {
            _contextRequestedItem = args.Item;
        }
    }
}

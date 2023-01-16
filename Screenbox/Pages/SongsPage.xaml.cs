using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SongsPage : Page
    {
        internal MusicPageViewModel ViewModel => (MusicPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public SongsPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<MusicPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        private void SongListView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ItemFlyout.Items == null) return;
            if (e.OriginalSource is FrameworkElement { DataContext: MediaViewModel media } element)
            {
                foreach (MenuFlyoutItemBase itemBase in ItemFlyout.Items)
                {
                    itemBase.DataContext = media;
                }

                ItemFlyout.ShowAt(element, e.GetPosition(element));
                e.Handled = true;
            }
        }

        private void SongListView_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            if (ItemFlyout.Items == null) return;
            if (args.OriginalSource is SelectorItem item)
            {
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

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicPage : ContentPage
    {
        internal MusicPageViewModel ViewModel => (MusicPageViewModel)DataContext;

        private ListViewItem? _focusedItem;

        public MusicPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<MusicPageViewModel>();
        }

        private async void MusicPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Loading", true);
            await ViewModel.FetchSongsAsync();
            SongsSource.Source = ViewModel.GroupedSongs;
            VisualStateManager.GoToState(this, ViewModel.GroupedSongs?.Count > 0 ? "Normal" : "NoContent", true);
        }

        private void SongListView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase > 0) return;

            // Remove handlers to prevent duplicated triggering
            args.ItemContainer.PointerEntered -= ItemContainerOnPointerEntered;
            args.ItemContainer.GettingFocus -= ItemContainerOnGotFocus;
            args.ItemContainer.FocusEngaged -= ItemContainerOnFocusEngaged;

            args.ItemContainer.PointerExited -= ItemContainerOnPointerExited;
            args.ItemContainer.PointerCanceled -= ItemContainerOnPointerExited;
            args.ItemContainer.LostFocus -= ItemContainerOnLostFocus;

            args.ItemContainer.DoubleTapped -= ItemContainerOnDoubleTapped;
            args.ItemContainer.SizeChanged -= ItemContainerOnSizeChanged;

            // Registering events
            args.ItemContainer.PointerEntered += ItemContainerOnPointerEntered;
            args.ItemContainer.GettingFocus += ItemContainerOnGotFocus;
            args.ItemContainer.FocusEngaged += ItemContainerOnFocusEngaged;

            args.ItemContainer.PointerExited += ItemContainerOnPointerExited;
            args.ItemContainer.PointerCanceled += ItemContainerOnPointerExited;
            args.ItemContainer.LostFocus += ItemContainerOnLostFocus;

            args.ItemContainer.DoubleTapped += ItemContainerOnDoubleTapped;
            args.ItemContainer.SizeChanged += ItemContainerOnSizeChanged;
        }

        private void ItemContainerOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item.ContentTemplateRoot is not Control control) return;
            if (item.Content is MediaViewModel media && media.MusicProperties == null)
            {
                VisualStateManager.GoToState(control, "Level0", true);
                return;
            }

            if (e.NewSize.Width > 800)
            {
                VisualStateManager.GoToState(control, "Level3", true);
            }
            else if (e.NewSize.Width > 620)
            {
                VisualStateManager.GoToState(control, "Level2", true);
            }
            else
            {
                VisualStateManager.GoToState(control, "Level1", true);
            }
        }

        private void ItemContainerOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item.Content is MediaViewModel selectedMedia)
            {
                ViewModel.PlayCommand.Execute(selectedMedia);
            }
        }

        private void ItemContainerOnFocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            sender.FindDescendant<Button>()?.Focus(FocusState.Programmatic);
        }

        private void ItemContainerOnGotFocus(object sender, RoutedEventArgs e)
        {
            _focusedItem = (ListViewItem)sender;
            ItemContainerOnPointerEntered(sender, e);
        }

        private void ItemContainerOnLostFocus(object sender, RoutedEventArgs e)
        {
            Control? control = FocusManager.GetFocusedElement() as Control;
            ListViewItem? item = control?.FindAscendantOrSelf<ListViewItem>();
            if (item == null || item != sender)
            {
                if (item == null) _focusedItem = null;
                ItemContainerOnPointerExited(sender, e);
            }
        }

        private void ItemContainerOnPointerExited(object sender, RoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            if (item == _focusedItem) return;
            Control? control = (Control?)item.ContentTemplateRoot;
            if (control == null) return;
            VisualStateManager.GoToState(control, "Normal", false);
        }

        private void ItemContainerOnPointerEntered(object sender, RoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            Control? control = (Control?)item.ContentTemplateRoot;
            if (control == null) return;
            VisualStateManager.GoToState(control, "PointerOver", false);
        }
    }
}

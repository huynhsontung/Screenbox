#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PlaylistView : UserControl
    {
        internal PlaylistViewModel ViewModel => (PlaylistViewModel)DataContext;

        private ListViewItem? _focusedItem;

        public PlaylistView()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlaylistViewModel>();
        }

        private void PlaylistListView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase > 0) return;

            // Remove handlers to prevent duplicated triggering
            args.ItemContainer.PointerEntered -= ItemContainerOnPointerEntered;
            args.ItemContainer.GettingFocus -= ItemContainerOnGotFocus;
            args.ItemContainer.FocusEngaged -= ItemContainerOnFocusEngaged;

            args.ItemContainer.PointerExited -= ItemContainerOnPointerExited;
            args.ItemContainer.PointerCanceled -= ItemContainerOnPointerExited;
            args.ItemContainer.LostFocus -= ItemContainerOnLostFocus;

            args.ItemContainer.DoubleTapped -= ViewModel.OnItemDoubleTapped;

            // Registering events
            args.ItemContainer.PointerEntered += ItemContainerOnPointerEntered;
            args.ItemContainer.GettingFocus += ItemContainerOnGotFocus;
            args.ItemContainer.FocusEngaged += ItemContainerOnFocusEngaged;

            args.ItemContainer.PointerExited += ItemContainerOnPointerExited;
            args.ItemContainer.PointerCanceled += ItemContainerOnPointerExited;
            args.ItemContainer.LostFocus += ItemContainerOnLostFocus;

            args.ItemContainer.DoubleTapped += ViewModel.OnItemDoubleTapped;
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

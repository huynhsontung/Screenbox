#nullable enable

using System.Collections.Specialized;
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
        public bool IsFlyout { get; set; }

        internal PlaylistViewModel ViewModel => (PlaylistViewModel)DataContext;

        private ListViewItem? _focusedItem;

        public PlaylistView()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlaylistViewModel>();
            ViewModel.Playlist.CollectionChanged += PlaylistOnCollectionChanged;
        }

        private void PlaylistOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel.Playlist.Count == 0)
            {
                MultiSelectToggle.IsChecked = false;
            }
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
                VisualStateManager.GoToState(control, "Minimal", true);
                return;
            }

            if (e.NewSize.Width > 620)
            {
                VisualStateManager.GoToState(control, "Full", true);
            }
            else
            {
                VisualStateManager.GoToState(control, "Compact", true);
            }
        }

        private void ItemContainerOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MultiSelectToggle.IsChecked ?? false) return;
            ViewModel.OnItemDoubleTapped(sender, e);
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

        private void SelectionCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectionCheckBox.IsChecked ?? false)
            {
                PlaylistListView.SelectAll();
            }
            else
            {
                PlaylistListView.SelectedItems.Clear();
            }
        }

        private void PlaylistListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionCheckBox.IsChecked = PlaylistListView.SelectedItems.Count == ViewModel.Playlist.Count;
            if (MultiSelectToggle.IsChecked ?? false)
            {
                VisualStateManager.GoToState(this,
                    PlaylistListView.SelectedItems.Count == 1 ? "MultipleSingleSelected" : "Multiple", true);
            }

            ViewModel.OnSelectionChanged(sender, e);
        }

        private void ClearSelection_OnClick(object sender, RoutedEventArgs e)
        {
            MultiSelectToggle.IsChecked = false;
            PlaylistListView.SelectedItem = null;
        }

        private void PlaylistView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsFlyout)
            {
                VisualStateManager.GoToState(this, "Minimal", true);
            }
        }

        private void CommandBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsFlyout) return;
            VisualStateManager.GoToState(this, e.NewSize.Width <= 620 ? "Compact" : "Normal", true);
        }
    }
}

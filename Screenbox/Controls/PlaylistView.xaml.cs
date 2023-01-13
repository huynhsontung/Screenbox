#nullable enable

using System.Collections.Specialized;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PlaylistView : UserControl
    {
        public static readonly DependencyProperty IsFlyoutProperty = DependencyProperty.Register(
            "IsFlyout",
            typeof(bool),
            typeof(PlaylistView),
            new PropertyMetadata(false));

        public bool IsFlyout
        {
            get => (bool)GetValue(IsFlyoutProperty);
            set => SetValue(IsFlyoutProperty, value);
        }

        internal PlaylistViewModel ViewModel => (PlaylistViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public PlaylistView()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlaylistViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        public async Task SmoothScrollActiveItemIntoViewAsync()
        {
            if (ViewModel.Playlist.CurrentItem == null || !ViewModel.HasItems) return;
            await PlaylistListView.SmoothScrollIntoViewWithItemAsync(ViewModel.Playlist.CurrentItem, ScrollItemPlacement.Center);
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
            SelectionCheckBox.IsChecked = PlaylistListView.SelectedItems.Count == ViewModel.Playlist.Items.Count;
            if (ViewModel.EnableMultiSelect)
            {
                VisualStateManager.GoToState(this,
                    PlaylistListView.SelectedItems.Count == 1 ? "MultipleSingleSelected" : "Multiple", true);
            }

            ViewModel.SelectionCount = PlaylistListView.SelectedItems.Count;
        }

        private async void PlaylistListView_OnDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            await ViewModel.EnqueueDataView(e.DataView);
        }

        private void PlaylistListView_OnDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = Strings.Resources.AddToQueue;
            }
        }

        private void CommandBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayoutState();
        }

        private void UpdateLayoutState()
        {
            if (IsFlyout)
            {
                VisualStateManager.GoToState(this, "Minimal", true);
                return;
            }

            VisualStateManager.GoToState(this, SelectionCommandBar.ActualWidth <= 620 ? "Compact" : "Normal", true);
        }

        private void GoToCurrentItem()
        {
            if (ViewModel.Playlist.CurrentItem != null && PlaylistListView.FindChild<ListViewBase>() is { } listView)
            {
                listView.SmoothScrollIntoViewWithItemAsync(ViewModel.Playlist.CurrentItem, ScrollItemPlacement.Center);
            }
        }

        private void PlaylistListView_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            if (ItemFlyout.Items == null) return;
            if (args.OriginalSource is ListViewItem item)
            {
                foreach (MenuFlyoutItemBase itemBase in ItemFlyout.Items)
                {
                    itemBase.DataContext = item.Content;
                }

                ItemFlyout.ShowAt(item);
                args.Handled = true;
            }
        }

        private void PlaylistListView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
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

        private void PlaylistView_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateLayoutState();
            GoToCurrentItem();
        }
    }
}

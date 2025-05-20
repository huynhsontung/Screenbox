#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Core.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

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
            DataContext = Ioc.Default.GetRequiredService<PlaylistViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
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
                VisualStateManager.GoToState(this, "Multiple", true);
            }

            ViewModel.SelectionCount = PlaylistListView.SelectedItems.Count;
        }

        internal async void PlaylistListView_OnDrop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
            e.Handled = true;
            IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
            if (items?.Count > 0)
            {
                await ViewModel.Playlist.EnqueueAsync(items);
            }
        }

        internal void PlaylistListView_OnDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
                ? DataPackageOperation.Copy
                : DataPackageOperation.None;
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
            if (ViewModel.Playlist.CurrentItem != null)
            {
                PlaylistListView.SmoothScrollIntoViewWithItemAsync(ViewModel.Playlist.CurrentItem, ScrollItemPlacement.Center);
                (PlaylistListView.ContainerFromItem(ViewModel.Playlist.CurrentItem) as Control)?.Focus(FocusState.Programmatic);
            }
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PlaylistViewModel.SelectionCount)) return;
            if (ViewModel.SelectionCount == 0) PlaylistListView.SelectedItems.Clear();
        }

        private void PlaylistView_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateLayoutState();
            GoToCurrentItem();

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void PlaylistView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        private void SelectDeselectAllKeyboardAccelerator_OnInvoked(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            if (PlaylistListView.Items.Count > 0)
            {
                if (PlaylistListView.SelectedItems.Count != ViewModel.Playlist.Items.Count)
                {
                    MultiSelectToggle.IsChecked = true;
                    PlaylistListView.SelectRange(new ItemIndexRange(0, (uint)PlaylistListView.Items.Count));
                    args.Handled = true;
                }
                else
                {
                    PlaylistListView.DeselectRange(new ItemIndexRange(0, (uint)PlaylistListView.Items.Count));
                    args.Handled = true;
                }
            }
        }

        /// <summary>
        /// Gets the tooltip text for a selection checkbox based on its current state.
        /// </summary>
        /// <param name="value">A nullable boolean representing the <see cref="CheckBox"/> state.</param>
        /// <returns>
        /// <strong>SelectNoneToolTip</strong> if the ToggleButton is checked; <strong>SelectAllToolTip</strong> if the ToggleButton is unchecked;
        /// otherwise throw not implemented exception.
        /// </returns>
        /// <exception cref="NotImplementedException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
        private string GetSelectionCheckBoxToolTip(bool? value)
        {
            return value is null
                ? throw new NotImplementedException()
                : (value.Value ? Strings.Resources.SelectNoneToolTip : Strings.Resources.SelectAllToolTip);
        }
    }
}

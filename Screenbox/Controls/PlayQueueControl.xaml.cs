#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Core.ViewModels;
using Screenbox.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PlayQueueControl : UserControl
    {
        public static readonly DependencyProperty IsFlyoutProperty = DependencyProperty.Register(
            "IsFlyout",
            typeof(bool),
            typeof(PlayQueueControl),
            new PropertyMetadata(false));

        public bool IsFlyout
        {
            get => (bool)GetValue(IsFlyoutProperty);
            set => SetValue(IsFlyoutProperty, value);
        }

        internal PlayQueueViewModel ViewModel => (PlayQueueViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private readonly Commands.SelectDeselectAllCommand _selectionCommand = new();

        public PlayQueueControl()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayQueueViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        public async Task SmoothScrollActiveItemIntoViewAsync()
        {
            if (ViewModel.Playlist.CurrentItem == null || !ViewModel.HasItems) return;
            await PlaylistListView.SmoothScrollIntoViewWithItemAsync(ViewModel.Playlist.CurrentItem, ScrollItemPlacement.Center);
        }

        private void PlaylistListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedCount = PlaylistListView.SelectedItems.Count;
            SelectionCheckBox.IsChecked = selectedCount == 0
                ? false
                : selectedCount == PlaylistListView.Items.Count ? true : null;
            ToolTipService.SetToolTip(SelectionCheckBox, GetSelectionCheckBoxToolTip(SelectionCheckBox.IsChecked));

            if (ViewModel.EnableMultiSelect)
            {
                VisualStateManager.GoToState(this, "Multiple", true);
            }

            ViewModel.SelectionCount = selectedCount;
        }

        internal async void PlaylistListView_OnDrop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
            e.Handled = true;
            IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
            if (items?.Count > 0)
            {
                int insertIndex = PlaylistListView.GetDropIndex(e) - 1;
                await ViewModel.Playlist.EnqueueAsync(items, insertIndex);
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
            if (e.PropertyName != nameof(PlayQueueViewModel.SelectionCount)) return;
            if (ViewModel.SelectionCount == 0) PlaylistListView.SelectedItems.Clear();
        }

        private void PlayQueue_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateLayoutState();
            GoToCurrentItem();

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void PlayQueue_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        private void SelectDeselectAllKeyboardAccelerator_OnInvoked(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            if (ViewModel.HasItems)
            {
                if (_selectionCommand.CanToggleSelection(PlaylistListView))
                {
                    ViewModel.EnableMultiSelect = true;
                    _selectionCommand.ToggleSelection(PlaylistListView);
                    args.Handled = true;
                }
            }
        }

        /// <summary>
        /// Gets the tooltip text for a selection checkbox based on its current state.
        /// </summary>
        /// <param name="value">A nullable boolean representing the <see cref="CheckBox"/> state.</param>
        /// <returns>
        /// <strong>SelectNoneToolTip</strong> if the ToggleButton is checked; <strong>SelectAllToolTip</strong> if the ToggleButton is unchecked or
        /// intermediate.
        /// </returns>
        private string GetSelectionCheckBoxToolTip(bool? value)
        {
            return value is true
                ? Strings.Resources.SelectNoneToolTip
                : Strings.Resources.SelectAllToolTip;
        }
    }
}

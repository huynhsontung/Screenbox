#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Commands;
using Screenbox.Core.ViewModels;
using Screenbox.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

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

    private readonly SelectDeselectAllCommand _selectionCommand;

    public PlayQueueControl()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayQueueViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        _selectionCommand = new SelectDeselectAllCommand();
    }

    public async Task SmoothScrollActiveItemIntoViewAsync()
    {
        if (ViewModel.Playlist.CurrentItem == null || !ViewModel.HasItems) return;
        await PlaylistListView.SmoothScrollIntoViewWithItemAsync(ViewModel.Playlist.CurrentItem, ScrollItemPlacement.Center);
    }

    internal async void PlaylistListView_OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        e.Handled = true;
        IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
        if (items?.Count > 0)
        {
            int insertIndex = PlaylistListView.GetDropIndex(e);
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

    private void PlayQueue_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLayoutState();
        GoToCurrentItem();
    }

    private void SelectDeselectAllKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.HasItems && _selectionCommand.CanToggleSelection(PlaylistListView))
        {
            ViewModel.Selection.IsSelectionModeActive = true;
            _selectionCommand.ToggleSelection(PlaylistListView);
            args.Handled = true;
        }
    }
}

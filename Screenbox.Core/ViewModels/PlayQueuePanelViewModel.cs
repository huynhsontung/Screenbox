#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// ViewModel for the play queue panel / flyout UI.
/// Handles item selection, reordering, and adding files or URLs to the queue.
/// </summary>
/// <remarks>
/// Queue state (items, current item) is provided by <see cref="PlayQueueContext"/>.
/// Mutations (remove, insert, enqueue) are delegated to <see cref="IPlayQueueCoordinator"/>.
/// </remarks>
public sealed partial class PlayQueuePanelViewModel : ObservableRecipient
{
    [ObservableProperty]
    private MediaViewModel? _contextMedia;

    /// <summary>The observable play queue state for data binding.</summary>
    public PlayQueueContext Queue { get; }

    public SelectionViewModel Selection { get; }

    public bool HasItems
    {
        get => _hasItems;
        private set => SetProperty(ref _hasItems, value);
    }

    private bool _hasItems;

    private readonly IPlayQueueCoordinator _coordinator;
    private readonly IFilesService _filesService;
    private readonly DispatcherQueue _dispatcherQueue;

    public PlayQueuePanelViewModel(
        PlayQueueContext queue,
        IPlayQueueCoordinator coordinator,
        SelectionViewModel selection,
        IFilesService filesService)
    {
        Queue = queue;
        _coordinator = coordinator;
        Selection = selection;
        _filesService = filesService;
        _hasItems = queue.Items.Count > 0;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Queue.Items.CollectionChanged += ItemsOnCollectionChanged;

        Selection.SetItemsSource(Queue.Items);
        ((INotifyCollectionChanged)Selection.SelectedRanges).CollectionChanged += Selection_SelectedRangesChanged;
    }

    private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasItems = Queue.Items.Count > 0;
        if (!HasItems)
        {
            Selection.IsSelectionModeActive = false;
        }
    }

    private void Selection_SelectedRangesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PlaySelectedNextCommand.NotifyCanExecuteChanged();
        RemoveSelectedCommand.NotifyCanExecuteChanged();
        MoveSelectedItemUpCommand.NotifyCanExecuteChanged();
        MoveSelectedItemDownCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Clear() => _coordinator.Clear();

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveSelected()
    {
        List<MediaViewModel> copy = Selection.GetSelectedItems<MediaViewModel>();
        Selection.ClearSelection();
        foreach (MediaViewModel item in copy)
        {
            Remove(item);
        }
    }

    [RelayCommand]
    private void Remove(MediaViewModel item)
    {
        _coordinator.Remove(item);
    }

    [RelayCommand]
    private void PlaySingle(MediaViewModel media)
    {
        if (Queue.CurrentItem == media && media.IsPlaying)
        {
            Messenger.Send(new TogglePlayPauseMessage(false));
        }
        else
        {
            Messenger.Send(new PlayMediaMessage(media, true));
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelectedNext()
    {
        List<MediaViewModel> reverse = Selection.GetSelectedItems<MediaViewModel>();
        if (reverse.Count == 0) return;
        reverse.Reverse();
        Selection.ClearSelection();
        foreach (MediaViewModel item in reverse)
        {
            PlayNext(item);
        }
    }

    [RelayCommand]
    private void PlayNext(MediaViewModel item)
    {
        _coordinator.InsertNext(item);
    }

    [RelayCommand(CanExecute = nameof(IsSelectedItemNotFirst))]
    private void MoveSelectedItemUp()
    {
        if (!IsSelectedItemNotFirst()) return;
        int oldIndex = Selection.SelectedRanges[0].FirstIndex;
        var item = Queue.Items[oldIndex];

        // Preserve selection.
        // When inserting before the selected, ListView selection is briefly out of sync.
        // Insert first. Remove. Then reselect.
        int newIndex = oldIndex - 1;
        Queue.Items.Insert(newIndex, item);
        Queue.Items.RemoveAt(oldIndex + 1);
        Selection.SelectRange(new Windows.UI.Xaml.Data.ItemIndexRange(newIndex, 1));
    }

    [RelayCommand(CanExecute = nameof(IsItemNotFirst))]
    private void MoveItemUp(MediaViewModel item)
    {
        int index = Queue.Items.IndexOf(item);
        if (index <= 0) return;
        Queue.Items.RemoveAt(index);
        Queue.Items.Insert(index - 1, item);
    }

    [RelayCommand(CanExecute = nameof(IsSelectedItemNotLast))]
    private void MoveSelectedItemDown()
    {
        if (!IsSelectedItemNotLast()) return;
        int oldIndex = Selection.SelectedRanges[0].FirstIndex;
        var item = Queue.Items[oldIndex];

        // Preserve selection. Insert first. Reselect. Remove after.
        int newIndex = oldIndex + 2;
        Queue.Items.Insert(newIndex, item);
        Selection.SelectRange(new Windows.UI.Xaml.Data.ItemIndexRange(newIndex, 1));
        Queue.Items.RemoveAt(oldIndex);
    }

    [RelayCommand(CanExecute = nameof(IsItemNotLast))]
    private void MoveItemDown(MediaViewModel item)
    {
        int index = Queue.Items.IndexOf(item);
        if (index == -1 || index >= Queue.Items.Count - 1) return;
        Queue.Items.RemoveAt(index);
        Queue.Items.Insert(index + 1, item);
    }

    /// <summary>
    /// Opens a file picker for the user to select files to add to the play queue.
    /// Sends a <see cref="Core.Messages.FailedToOpenFilesNotificationMessage"/> on failure.
    /// </summary>
    [RelayCommand]
    private async Task AddFilesAsync()
    {
        try
        {
            IReadOnlyList<StorageFile>? files = await _filesService.PickMultipleFilesAsync();
            if (files is null || files.Count == 0) return;
            await _coordinator.EnqueueAsync(files);
        }
        catch (Exception e)
        {
            Messenger.Send(new FailedToOpenFilesNotificationMessage(e.Message));
        }
    }

    /// <summary>
    /// Enqueues storage items dropped onto the queue at the specified position.
    /// </summary>
    public Task EnqueueDroppedItemsAsync(IReadOnlyList<IStorageItem> items, int insertIndex) =>
        _coordinator.EnqueueAsync(items, insertIndex);

    private bool HasSelection() => Selection.SelectedRanges.Count > 0;

    private bool IsSelectionSingle() => Selection.SelectedRanges.Count == 1 && Selection.SelectedRanges[0] is { Length: 1 };

    private bool IsSelectedItemNotFirst() =>
        IsSelectionSingle() &&
        Queue.Items.Count > 0 &&
        Selection.SelectedRanges[0].FirstIndex > 0;

    private bool IsSelectedItemNotLast() =>
        IsSelectionSingle() &&
        Queue.Items.Count > 0 &&
        Selection.SelectedRanges[0].LastIndex < Queue.Items.Count - 1;

    private bool IsItemNotFirst(MediaViewModel item) => Queue.Items.Count > 0 && Queue.Items[0] != item;

    private bool IsItemNotLast(MediaViewModel item) => Queue.Items.Count > 0 && Queue.Items[Queue.Items.Count - 1] != item;
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
    /// <summary>The observable play queue state for data binding.</summary>
    public PlayQueueContext Queue { get; }

    public SelectionViewModel Selection { get; }

    /// <summary>Clears the entire play queue. Delegated to <see cref="IPlayQueueCoordinator"/>.</summary>
    public IRelayCommand ClearCommand => _coordinator.ClearCommand;

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
        Selection.PropertyChanged += Selection_OnPropertyChanged;
    }

    private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        HasItems = Queue.Items.Count > 0;
        if (!HasItems)
        {
            Selection.IsSelectionModeActive = false;
        }
    }

    private void Selection_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selection.IsAllSelected))
        {
            PlaySelectedNextCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            MoveSelectedItemUpCommand.NotifyCanExecuteChanged();
            MoveSelectedItemDownCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveSelected(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;
        List<object> copy = selectedItems.ToList();
        selectedItems.Clear();
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
        if (Queue.CurrentItem == media && (media.IsPlaying ?? false))
        {
            Messenger.Send(new TogglePlayPauseMessage(false));
        }
        else
        {
            Messenger.Send(new PlayMediaMessage(media, true));
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelectedNext(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;
        List<object> reverse = selectedItems.Reverse().ToList();
        selectedItems.Clear();
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
    private void MoveSelectedItemUp(IList<object>? selectedItems)
    {
        if (selectedItems is not { Count: 1 }) return;
        MediaViewModel item = (MediaViewModel)selectedItems[0];
        MoveItemUp(item);

        // Re-select the item after the move. Delaying ensures the ListView has
        // processed the collection change before we add back the selection;
        // doing it synchronously would cause the entire list to reload.
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => selectedItems.Add(item));
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
    private void MoveSelectedItemDown(IList<object>? selectedItems)
    {
        if (selectedItems is not { Count: 1 }) return;
        MediaViewModel item = (MediaViewModel)selectedItems[0];
        MoveItemDown(item);

        // Re-select the item after the move. Same reasoning as MoveSelectedItemUp.
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => selectedItems.Add(item));
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

    private bool HasSelection() => Selection.SelectedItems.Count > 0;

    private bool IsSelectedItemNotFirst(IList<object>? selectedItems) =>
        selectedItems?.Count == 1 &&
        Queue.Items.Count > 0 && Queue.Items[0] != selectedItems[0];

    private bool IsSelectedItemNotLast(IList<object>? selectedItems) =>
        selectedItems?.Count == 1 &&
        Queue.Items.Count > 0 && Queue.Items[Queue.Items.Count - 1] != selectedItems[0];

    private bool IsItemNotFirst(MediaViewModel item) => Queue.Items.Count > 0 && Queue.Items[0] != item;

    private bool IsItemNotLast(MediaViewModel item) => Queue.Items.Count > 0 && Queue.Items[Queue.Items.Count - 1] != item;
}

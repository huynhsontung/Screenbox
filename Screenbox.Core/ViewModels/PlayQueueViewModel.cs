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
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlayQueueViewModel : ObservableRecipient
{
    public MediaListViewModel Playlist { get; }

    public SelectionViewModel Selection { get; }

    public bool HasItems
    {
        get => _hasItems;
        private set
        {
            SetProperty(ref _hasItems, value);
        }
    }

    private bool _hasItems;

    private readonly IFilesService _filesService;
    private readonly DispatcherQueue _dispatcherQueue;

    public PlayQueueViewModel(MediaListViewModel playlist, SelectionViewModel selection, IFilesService filesService)
    {
        Playlist = playlist;
        Selection = selection;
        _filesService = filesService;
        _hasItems = playlist.Items.Count > 0;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Playlist.Items.CollectionChanged += ItemsOnCollectionChanged;

        Selection.IsAllSelected = Playlist.Items.GetSelectionToggleState(Selection.SelectedItemCount);
        Selection.PropertyChanged += Selection_OnPropertyChanged;
    }

    private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        HasItems = Playlist.Items.Count > 0;
        if (!HasItems)
        {
            Selection.IsSelectionModeActive = false;
        }
    }

    private void Selection_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selection.SelectedItemCount))
        {
            Selection.IsAllSelected = Playlist.Items.GetSelectionToggleState(Selection.SelectedItemCount);
            PlaySelectedNextCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
            MoveSelectedItemUpCommand.NotifyCanExecuteChanged();
            MoveSelectedItemDownCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveSelected(IList<object>? selectedItems)
    {
        if (selectedItems == null) return;
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
        if (Playlist.CurrentItem == item)
        {
            Playlist.CurrentItem = null;
        }

        Playlist.Items.Remove(item);
    }

    [RelayCommand]
    private void PlaySingle(MediaViewModel media)
    {
        if (Playlist.CurrentItem == media && (media.IsPlaying ?? false))
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
        if (selectedItems == null) return;
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
        Playlist.Items.Insert(Playlist.CurrentIndex + 1, new MediaViewModel(item));
    }

    [RelayCommand(CanExecute = nameof(IsSelectedItemNotFirst))]
    private void MoveSelectedItemUp(IList<object>? selectedItems)
    {
        if (selectedItems is not { Count: 1 }) return;
        MediaViewModel item = (MediaViewModel)selectedItems[0];
        MoveItemUp(item);

        // Selected items will be empty after move
        // Delay adding the items back to selected so the items have the chance to update first
        // If this order is not followed, the whole listview will reload
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => selectedItems.Add(item));
    }

    [RelayCommand(CanExecute = nameof(IsItemNotFirst))]
    private void MoveItemUp(MediaViewModel item)
    {
        int index = Playlist.Items.IndexOf(item);
        if (index <= 0) return;
        Playlist.Items.RemoveAt(index);
        Playlist.Items.Insert(index - 1, item);
    }

    [RelayCommand(CanExecute = nameof(IsSelectedItemNotLast))]
    private void MoveSelectedItemDown(IList<object>? selectedItems)
    {
        if (selectedItems is not { Count: 1 }) return;
        MediaViewModel item = (MediaViewModel)selectedItems[0];
        MoveItemDown(item);

        // Selected items will be empty after move
        // Delay adding the items back to selected so the items have the chance to update first
        // If this order is not followed, the whole listview will reload
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => selectedItems.Add(item));
    }

    [RelayCommand(CanExecute = nameof(IsItemNotLast))]
    private void MoveItemDown(MediaViewModel item)
    {
        int index = Playlist.Items.IndexOf(item);
        if (index == -1 || index >= Playlist.Items.Count - 1) return;
        Playlist.Items.RemoveAt(index);
        Playlist.Items.Insert(index + 1, item);
    }

    [RelayCommand]
    private void ClearSelection()
    {
        Selection.ClearSelection();
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
            if (files == null || files.Count == 0) return;
            await Playlist.EnqueueAsync(files);
        }
        catch (Exception e)
        {
            Messenger.Send(new FailedToOpenFilesNotificationMessage(e.Message));
        }
    }

    private bool HasSelection(IList<object>? selectedItems) =>
        (selectedItems != null && selectedItems.Count > 0) || Selection.HasSelection;

    private bool IsSelectedItemNotFirst(IList<object>? selectedItems) =>
        selectedItems?.Count == 1 &&
        Playlist.Items.Count > 0 && Playlist.Items[0] != selectedItems[0];

    private bool IsSelectedItemNotLast(IList<object>? selectedItems) =>
        selectedItems?.Count == 1 &&
        Playlist.Items.Count > 0 && Playlist.Items[Playlist.Items.Count - 1] != selectedItems[0];

    private bool IsItemNotFirst(MediaViewModel item) => Playlist.Items.Count > 0 && Playlist.Items[0] != item;

    private bool IsItemNotLast(MediaViewModel item) => Playlist.Items.Count > 0 && Playlist.Items[Playlist.Items.Count - 1] != item;
}

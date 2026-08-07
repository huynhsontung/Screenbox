#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Helpers;
using Windows.UI.Xaml.Data;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Provides selection state and helpers for view models that support item selection.
/// Assumes <see cref="SelectedRanges"/> is always maintained in a compacted and ordered state.
/// </summary>
public sealed partial class SelectionViewModel : ObservableObject
{
    private readonly ObservableCollection<ItemIndexRange> _selectedRanges;

    /// <summary>
    /// Gets the read-only collection of currently selected item index ranges.
    /// Assumed to be always ordered and non-overlapping.
    /// </summary>
    public ReadOnlyObservableCollection<ItemIndexRange> SelectedRanges { get; }

    /// <summary>
    /// Gets or sets the total count of distinct selected items across all ranges.
    /// </summary>
    [ObservableProperty]
    private int _selectedCount;

    /// <summary>
    /// Gets or sets a value that indicates whether all items are selected.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if all items are selected; <see langword="false"/> if none
    /// are selected; otherwise, <see langword="null"/> to indicate a mixed selection.
    /// The default is <see langword="false"/>.
    /// </value>
    [ObservableProperty]
    private bool? _isAllSelected = false;

    /// <summary>
    /// Gets or sets a value that indicates whether selection mode is active.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if selection mode is active; otherwise, <see langword="false"/>.
    /// The default is <see langword="false"/>.
    /// </value>
    [ObservableProperty]
    private bool _isSelectionModeActive;

    private IReadOnlyList<object>? _sourceCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionViewModel"/> class.
    /// </summary>
    public SelectionViewModel()
    {
        _selectedRanges = new ObservableCollection<ItemIndexRange>();
        SelectedRanges = new ReadOnlyObservableCollection<ItemIndexRange>(_selectedRanges);
    }

    /// <summary>
    /// Sets the source collection for selection and updates the selection state.
    /// </summary>
    public void SetItemsSource(IReadOnlyList<object>? source)
    {
        if (_sourceCollection is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= SourceCollection_OnCollectionChanged;
        }

        _sourceCollection = source;

        if (_sourceCollection is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += SourceCollection_OnCollectionChanged;
        }

        CompactRanges();
        RefreshSelectionState();
    }

    /// <summary>
    /// Sets the selected ranges from an external collection, maintaining the compacted and ordered invariant.
    /// </summary>
    public void SetRanges(IEnumerable<ItemIndexRange> ranges)
    {
        var newRanges = GetCompactRanges(ranges);

        // Activate selection mode if there are any selected ranges and selection mode is not already active
        if (!IsSelectionModeActive && newRanges.Count > 0)
        {
            IsSelectionModeActive = true;
        }

        _selectedRanges.SyncItems(newRanges);
        SelectedCount = newRanges.Sum(r => (int)r.Length);
        RefreshSelectionState();
    }

    /// <summary>
    /// Retrieves a list of selected items from the configured source collection based on current selected ranges.
    /// </summary>
    public List<T> GetSelectedItems<T>()
    {
        var list = _sourceCollection;
        if (list is null || list.Count == 0 || _selectedRanges.Count == 0) return new List<T>();

        var selectedItems = new List<T>();
        foreach (var range in _selectedRanges)
        {
            int start = Math.Max(0, range.FirstIndex);
            int end = Math.Min(list.Count - 1, range.LastIndex);
            for (int i = start; i <= end; i++)
            {
                selectedItems.Add((T)list[i]);
            }
        }

        return selectedItems;
    }

    /// <summary>
    /// Retrieves a list of selected objects from the configured source collection based on current selected ranges.
    /// </summary>
    public List<object> GetSelectedItems()
    {
        if (_sourceCollection is null || _sourceCollection.Count == 0) return new List<object>();
        return GetSelectedItems<object>();
    }

    /// <summary>
    /// Selects a specified range of items, calculating the final merged ranges in memory first,
    /// then updating <see cref="SelectedRanges"/> in place with minimal collection mutations.
    /// </summary>
    public void SelectRange(ItemIndexRange range)
    {
        if (range.Length == 0) return;

        var newRanges = GetCompactRanges(_selectedRanges.Concat(new[] { range }));

        IsSelectionModeActive = true;
        _selectedRanges.SyncItems(newRanges);
        SelectedCount = newRanges.Sum(r => (int)r.Length);
        RefreshSelectionState();
    }

    /// <summary>
    /// Deselects a specified range of items, calculating remaining ranges in memory first,
    /// then updating <see cref="SelectedRanges"/> in place with minimal collection mutations.
    /// </summary>
    public void DeselectRange(ItemIndexRange range)
    {
        if (range.Length == 0 || _selectedRanges.Count == 0) return;

        var remaining = new List<ItemIndexRange>();
        foreach (var r in _selectedRanges)
        {
            // Fully outside
            if (r.LastIndex < range.FirstIndex || r.FirstIndex > range.LastIndex)
            {
                remaining.Add(r);
                continue;
            }

            // Left portion remaining
            if (r.FirstIndex < range.FirstIndex)
            {
                remaining.Add(new ItemIndexRange(r.FirstIndex, (uint)(range.FirstIndex - r.FirstIndex)));
            }

            // Right portion remaining
            if (r.LastIndex > range.LastIndex)
            {
                remaining.Add(new ItemIndexRange(range.LastIndex + 1, (uint)(r.LastIndex - range.LastIndex)));
            }
        }

        // When deselecting, we don't need to compact since we started with a compacted collection
        _selectedRanges.SyncItems(remaining);
        SelectedCount = remaining.Sum(r => (int)r.Length);
        // Don't disable selection mode since use may still want to select after clearing selection
        RefreshSelectionState();
    }

    /// <summary>
    /// Selects the specified item and activates selection mode.
    /// </summary>
    /// <param name="item">An object representing the item to select.</param>
    [RelayCommand]
    public void SelectItem(object? item)
    {
        int index = GetItemIndex(item);
        if (index >= 0)
        {
            SelectRange(new ItemIndexRange(index, 1));
        }
    }

    /// <summary>
    /// Deselects the specified item.
    /// </summary>
    /// <param name="item">An object representing the item to deselect.</param>
    [RelayCommand]
    public void DeselectItem(object? item)
    {
        int index = GetItemIndex(item);
        if (index >= 0)
        {
            DeselectRange(new ItemIndexRange(index, 1));
        }
    }

    public int GetItemIndex(object? item)
    {
        if (item is null || _sourceCollection is null) return -1;

        if (_sourceCollection is IList list)
        {
            return list.IndexOf(item);
        }

        int i = 0;
        foreach (var elem in _sourceCollection)
        {
            if (Equals(elem, item))
            {
                return i;
            }
            i++;
        }
        return -1;
    }

    public void ClearSelection()
    {
        _selectedRanges.Clear();
        SelectedCount = 0;
        RefreshSelectionState();
    }

    /// <summary>
    /// Clears the current selection and exits selection mode.
    /// </summary>
    [RelayCommand]
    public void DisableSelectionMode()
    {
        IsSelectionModeActive = false;
        ClearSelection();
    }

    private void SourceCollection_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CompactRanges();
        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        if (_sourceCollection is null) return;

        int totalCount = _sourceCollection.Count;
        int selectedCount = SelectedCount;
        if (selectedCount < 0 || selectedCount > totalCount) return;

        IsAllSelected = selectedCount == 0
            ? false
            : selectedCount == totalCount ? true : null;
    }

    /// <summary>
    /// Compacts and normalizes the given ranges in memory without mutating <see cref="SelectedRanges"/>.
    /// </summary>
    private List<ItemIndexRange> GetCompactRanges(IEnumerable<ItemIndexRange> ranges)
    {
        int sourceCount = _sourceCollection?.Count ?? int.MaxValue;

        var validRanges = new List<ItemIndexRange>();
        foreach (var r in ranges)
        {
            if (r.Length == 0 || r.FirstIndex < 0 || r.FirstIndex >= sourceCount)
                continue;

            // If the range is fully within the source collection, add it as is
            if (r.LastIndex < sourceCount)
            {
                validRanges.Add(r);
                continue;
            }

            // Range extends beyond the source collection, truncate it to the valid range
            int length = sourceCount - r.FirstIndex;
            if (length > 0)
            {
                validRanges.Add(new ItemIndexRange(r.FirstIndex, (uint)length));
            }
        }

        if (validRanges.Count == 0)
            return new List<ItemIndexRange>();

        var sorted = validRanges.OrderBy(r => r.FirstIndex).ToList();
        var merged = new List<ItemIndexRange>();
        ItemIndexRange current = sorted[0];

        for (int i = 1; i < sorted.Count; i++)
        {
            var next = sorted[i];
            // Merge overlapping or adjacent ranges
            // If the next range starts before or at the end of the current range, merge them  
            if (next.FirstIndex <= current.LastIndex + 1)
            {
                int newLast = Math.Max(current.LastIndex, next.LastIndex);
                int newFirst = current.FirstIndex;
                current = new ItemIndexRange(newFirst, (uint)(newLast - newFirst + 1));
            }
            else
            {
                // No overlap, add the current range and move to the next
                merged.Add(current);
                current = next;
            }
        }

        merged.Add(current);
        return merged;
    }

    /// <summary>
    /// Compacts and normalizes <see cref="SelectedRanges"/> in place using <see cref="CollectionExtensions.SyncItems"/>.
    /// </summary>
    private void CompactRanges()
    {
        if (_selectedRanges.Count == 0)
        {
            SelectedCount = 0;
            return;
        }

        var compacted = GetCompactRanges(_selectedRanges);
        _selectedRanges.SyncItems(compacted);
        SelectedCount = compacted.Sum(r => (int)r.Length);
    }

}

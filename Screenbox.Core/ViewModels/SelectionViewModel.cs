#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Provides selection state and helpers for view models that support item selection.
/// </summary>
/// <remarks>
/// This view model exposes selection-related properties such as the set of selected
/// items, whether selection mode is active, and the selection state for a collection
/// of items. It is intended to be composed into other view models to provide a
/// consistent selection experience across the application.
/// </remarks>
public sealed partial class SelectionViewModel : ObservableObject
{
    /// <summary>
    /// Gets the collection of currently selected items.
    /// </summary>
    /// <value>
    /// A collection containing the selected items. The default is an empty collection.
    /// </value>
    public ObservableCollection<object> SelectedItems { get; }

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

    private IReadOnlyCollection<object>? _sourceCollection;

    /// <inheritdoc cref="ObservableCollection{T}.CollectionChanged"/>
    public event NotifyCollectionChangedEventHandler? SelectedItemsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionViewModel"/> class.
    /// </summary>
    public SelectionViewModel()
    {
        SelectedItems = new ObservableCollection<object>();
        SelectedItems.CollectionChanged += SelectedItems_OnCollectionChanged;
    }

    /// <summary>
    /// Sets the source collection for selection and updates the selection state.
    /// </summary>
    /// <param name="source">A collection of items to be used as the selection source.</param>
    public void SetItemsSource(IReadOnlyCollection<object>? source)
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
    }

    /// <summary>
    /// Selects the specified item and activates selection mode.
    /// </summary>
    /// <param name="item">An object representing the item to select.</param>
    [RelayCommand]
    private void SelectItem(object? item)
    {
        if (item is null) return;

        IsSelectionModeActive = true;
        if (!SelectedItems.Contains(item))
        {
            SelectedItems.Add(item);
        }
    }

    /// <summary>
    /// Clears the current selection and exits selection mode.
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        IsSelectionModeActive = false;
        SelectedItems.Clear();
    }

    private void SelectedItems_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        SelectedItemsChanged?.Invoke(sender, e);
        RefreshSelectionState();
    }

    private void SourceCollection_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // Refresh the selection state to ensure it accurately reflects the current collection
        // (e.g., when removing the last unselected item).
        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        if (_sourceCollection is null) return;

        int totalCount = _sourceCollection.Count;
        int selectedCount = SelectedItems.Count;
        if (selectedCount < 0 || selectedCount > totalCount) return;

        IsAllSelected = selectedCount == 0
            ? false
            : selectedCount == totalCount ? true : null;
    }
}

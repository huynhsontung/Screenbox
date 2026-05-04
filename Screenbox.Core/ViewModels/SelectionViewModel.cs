#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Provides selection state and helpers for view models that support item selection.
/// </summary>
/// <remarks>
/// This view model exposes selection-related properties such as the currently selected
/// item, whether selection mode is active, and the count of selected items. It is
/// intended to be composed into other view models to provide a consistent selection
/// experience across the application.
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
    /// Gets a value that indicates whether there is at least one selected item.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="SelectedItemCount"/> is greater than <c>0</c>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasSelection => SelectedItemCount > 0;

    /// <summary>
    /// Gets or sets the number of selected items.
    /// </summary>
    /// <value>The current count of selected items. The default is <c>0</c>.</value>
    [ObservableProperty]
    private int _selectedItemCount;

    /// <summary>
    /// Gets or sets a value that indicates whether all items are selected.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if all items are selected; <see langword="false"/> if none
    /// are selected; otherwise <see langword="null"/> to indicate a mixed selection.
    /// </value>
    [ObservableProperty]
    private bool? _isAllSelected;

    /// <summary>
    /// Gets or sets a value that indicates whether selection mode is active.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if selection mode is active; otherwise, <see langword="false"/>.
    /// The default is <see langword="false"/>.
    /// </value>
    [ObservableProperty]
    private bool _isSelectionModeActive;

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    /// <value>
    /// The selected item, or <see langword="null"/> if no item is selected.
    /// </value>
    [ObservableProperty]
    private object? _selectedItem;

    public SelectionViewModel()
    {
        SelectedItems = new ObservableCollection<object>();
        SelectedItems.CollectionChanged += SelectedItems_OnCollectionChanged;
    }

    partial void OnSelectedItemCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasSelection));
    }

    partial void OnIsSelectionModeActiveChanged(bool value)
    {
        if (!value) SelectedItemCount = 0;
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
        SelectedItem = item;
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
        SelectedItem = null;
    }

    private void SelectedItems_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        int newCount = SelectedItems.Count;
        if (SelectedItemCount != newCount)
        {
            SelectedItemCount = newCount;
        }
    }
}

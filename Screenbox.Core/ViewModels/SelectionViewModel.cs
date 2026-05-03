#nullable enable

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents selection state used by views and view models.
/// </summary>
public sealed partial class SelectionViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectionCount;

    [ObservableProperty]
    private bool? _selectionCheckState;

    [ObservableProperty]
    private bool _enableMultiSelect;

    [ObservableProperty]
    private object? _selectedItemToAdd;

    partial void OnSelectionCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasSelection));
    }

    partial void OnEnableMultiSelectChanged(bool value)
    {
        if (!value) SelectionCount = 0;
    }

    public void ClearSelection()
    {
        EnableMultiSelect = false;
        SelectedItemToAdd = null;
        SelectionCount = 0;
    }

    public static bool HasSelection(IList<object>? selectedItems) => selectedItems?.Count > 0;
}

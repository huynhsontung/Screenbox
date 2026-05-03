#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents selection state used by views and view models.
/// </summary>
public sealed partial class SelectionViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedItemCount;

    [ObservableProperty]
    private bool? _isAllSelected;

    [ObservableProperty]
    private bool _isSelectionModeActive;

    [ObservableProperty]
    private object? _selectedItem;

    partial void OnSelectedItemCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasSelection));
    }

    partial void OnIsSelectionModeActiveChanged(bool value)
    {
        if (!value) SelectedItemCount = 0;
    }

    public void ClearSelection()
    {
        IsSelectionModeActive = false;
        SelectedItem = null;
        SelectedItemCount = 0;
    }

    public bool HasSelection => SelectedItemCount > 0;
}

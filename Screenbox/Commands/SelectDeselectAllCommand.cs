#nullable enable

using System;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Screenbox.Commands;

/// <summary>
/// Represents a command that toggles the selection state of all items in a
/// <see cref="ListViewBase"/> or <see cref="ListBox"/> control.
/// </summary>
public sealed class SelectDeselectAllCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    internal bool CanToggleSelection(ListViewBase listViewBase)
    {
        return listViewBase.SelectionMode
            is ListViewSelectionMode.Multiple
            or ListViewSelectionMode.Extended;
    }

    internal bool CanToggleSelection(ListBox listBox)
    {
        return listBox.SelectionMode is SelectionMode.Multiple or SelectionMode.Extended;
    }

    internal void ToggleSelection(ListViewBase listViewBase)
    {
        var allItemsRange = new ItemIndexRange(0, (uint)listViewBase.Items.Count);
        if (listViewBase.SelectedItems.Count != listViewBase.Items.Count)
        {
            listViewBase.SelectRange(allItemsRange);
        }
        else
        {
            listViewBase.DeselectRange(allItemsRange);
        }
    }

    internal void ToggleSelection(ListBox listBox)
    {
        if (listBox.SelectedItems.Count != listBox.Items.Count)
        {
            listBox.SelectAll();
        }
        else
        {
            listBox.SelectedItems.Clear();
        }
    }

    public bool CanExecute(object? parameter)
    {
        return parameter switch
        {
            ListViewBase listViewBase => CanToggleSelection(listViewBase),
            ListBox listBox => CanToggleSelection(listBox),
            _ => false
        };
    }

    public void Execute(object? parameter)
    {
        switch (parameter)
        {
            case ListViewBase listViewBase:
                ToggleSelection(listViewBase);
                break;
            case ListBox listBox:
                ToggleSelection(listBox);
                break;
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

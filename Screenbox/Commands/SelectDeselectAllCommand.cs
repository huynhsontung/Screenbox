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

    /// <summary>
    /// Determines whether the <see cref="ListViewBase"/> items can be selected.
    /// </summary>
    /// <param name="listViewBase">The <see cref="ListViewBase"/> to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="listViewBase"/> has a selection mode of
    /// <see cref="ListViewSelectionMode.Multiple"/> or <see cref="ListViewSelectionMode.Extended"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public bool CanToggleSelection(ListViewBase listViewBase)
    {
        return listViewBase.SelectionMode
            is ListViewSelectionMode.Multiple
            or ListViewSelectionMode.Extended;
    }

    /// <summary>
    /// Determines whether the <see cref="ListBox"/> items can be selected.
    /// </summary>
    /// <param name="listBox">The <see cref="ListBox"/> to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="listBox"/> has a selection behavior of
    /// <see cref="SelectionMode.Multiple"/> or <see cref="SelectionMode.Extended"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public bool CanToggleSelection(ListBox listBox)
    {
        return listBox.SelectionMode is not SelectionMode.Single;
    }

    /// <summary>
    /// Changes the selection state of all items in the specified <see cref="ListViewBase"/>.
    /// For example, if all items in the <paramref name="listViewBase"/> are currently selected,
    /// calling this method deselects all items.
    /// </summary>
    /// <param name="listViewBase">The target control whose selection state will be changed.</param>
    public void ToggleSelection(ListViewBase listViewBase)
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

    /// <summary>
    /// Changes the selection state of all items in the specified <see cref="ListBox"/>.
    /// For example, if all items in the <paramref name="listBox"/> are currently selected,
    /// calling this method deselects all items.
    /// </summary>
    /// <param name="listBox">The target control whose selection state will be changed.</param>
    public void ToggleSelection(ListBox listBox)
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

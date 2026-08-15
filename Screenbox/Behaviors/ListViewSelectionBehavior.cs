using System.Collections.Specialized;
using System.Linq;
using Microsoft.Xaml.Interactivity;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Behaviors;

/// <summary>
/// Provides a behavior that synchronizes a <see cref="SelectionViewModel"/> with the <see cref="ListViewBase.SelectedRanges"/> collection.
/// </summary>
internal sealed partial class ListViewSelectionBehavior : Behavior<ListViewBase>
{
    /// <summary>
    /// Identifies the <see cref="Selection"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(
        nameof(Selection),
        typeof(SelectionViewModel),
        typeof(ListViewSelectionBehavior),
        new PropertyMetadata(null, OnSelectionChanged));

    /// <summary>
    /// Gets or sets the selection view model associated with this behavior.
    /// </summary>
    public SelectionViewModel? Selection
    {
        get => (SelectionViewModel?)GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    private bool _isUpdating;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged += ListViewBase_OnSelectionChanged;
        if (Selection is { } selection)
        {
            ((INotifyCollectionChanged)selection.SelectedRanges).CollectionChanged += Selection_CollectionChanged;
            SyncVmToNative();
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectionChanged -= ListViewBase_OnSelectionChanged;
        if (Selection is { } selection)
        {
            ((INotifyCollectionChanged)selection.SelectedRanges).CollectionChanged -= Selection_CollectionChanged;
        }
    }

    private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListViewSelectionBehavior behavior)
        {
            if (e.OldValue is SelectionViewModel oldVm)
            {
                ((INotifyCollectionChanged)oldVm.SelectedRanges).CollectionChanged -= behavior.Selection_CollectionChanged;
            }

            if (e.NewValue is SelectionViewModel newVm)
            {
                ((INotifyCollectionChanged)newVm.SelectedRanges).CollectionChanged += behavior.Selection_CollectionChanged;
                behavior.SyncVmToNative();
            }
        }
    }

    private void ListViewBase_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SyncNativeToVm();
    }

    private void Selection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncVmToNative();
    }

    private void SyncNativeToVm()
    {
        if (_isUpdating || AssociatedObject is not { } listViewBase || Selection is not { } selection)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            var nativeRanges = listViewBase.SelectedRanges.ToList();
            selection.SetRanges(nativeRanges);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SyncVmToNative()
    {
        if (_isUpdating || AssociatedObject is not { } listViewBase || Selection is not { } selection)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            var vmRanges = selection.SelectedRanges.ToList();
            if (vmRanges.Count == 0)
            {
                foreach (var range in listViewBase.SelectedRanges.ToList())
                {
                    listViewBase.DeselectRange(range);
                }
                return;
            }

            var nativeRanges = listViewBase.SelectedRanges.ToList();

            // Deselect ranges no longer present in ViewModel
            foreach (var range in nativeRanges)
            {
                if (!vmRanges.Any(r => r.FirstIndex == range.FirstIndex && r.Length == range.Length))
                {
                    listViewBase.DeselectRange(range);
                }
            }

            // Select ranges present in ViewModel but not natively
            foreach (var range in vmRanges)
            {
                if (!nativeRanges.Any(r => r.FirstIndex == range.FirstIndex && r.Length == range.Length))
                {
                    listViewBase.SelectRange(range);
                }
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }
}

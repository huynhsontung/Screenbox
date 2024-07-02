using Microsoft.Xaml.Interactivity;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Interactions;
internal class GroupingOverviewBehavior : Behavior<GridView>
{
    public static readonly DependencyProperty GroupTypeProperty = DependencyProperty.Register(
        nameof(GroupType), typeof(string), typeof(GroupingOverviewBehavior), new PropertyMetadata(default(string), OnGroupTypeChanged));

    public string GroupType
    {
        get => (string)GetValue(GroupTypeProperty);
        set => SetValue(GroupTypeProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SizeChanged += AssociatedObjectOnSizeChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SizeChanged -= AssociatedObjectOnSizeChanged;
    }

    private static void OnGroupTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (GroupingOverviewBehavior)d;
        behavior.UpdateGroupViewItemWidth();
    }

    private void AssociatedObjectOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGroupViewItemWidth();
    }

    private void UpdateGroupViewItemWidth()
    {
        if (AssociatedObject.ItemsPanelRoot == null) return;
        var gridContentWidth = AssociatedObject.ActualWidth -
                               (AssociatedObject.Margin.Left + AssociatedObject.Margin.Right) -
                               (AssociatedObject.Padding.Left + AssociatedObject.Padding.Right);
        var numColumns = (int)gridContentWidth / 400;
        var itemWidth = numColumns > 0 ? gridContentWidth / numColumns : gridContentWidth;
        itemWidth -= 4; // Item paddings
        itemWidth = Math.Floor(itemWidth);

        foreach (var child in AssociatedObject.ItemsPanelRoot.Children)
        {
            var element = (FrameworkElement)child;
            element.Width = GroupType == "year"
                ? 80
                : AssociatedObject.HorizontalAlignment != HorizontalAlignment.Stretch
                    ? double.NaN
                    : itemWidth;
        }
    }
}

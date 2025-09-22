#nullable enable

using CommunityToolkit.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Extensions;

/// <summary>
/// Provides attached dependency properties for the <see cref="ListViewBase"/> control.
/// </summary>
public static class ListViewExtensions
{
    private static readonly bool IsApiContract13Present = Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 13);

    /// <summary>
    /// Identifies the attached dependency property that specifies the <see cref="CornerRadius"/> of <see cref="ListViewBase"/> item.
    /// </summary>
    public static readonly DependencyProperty ItemCornerRadiusProperty = DependencyProperty.RegisterAttached(
        "ItemCornerRadius",
        typeof(CornerRadius?),
        typeof(ListViewExtensions),
        new PropertyMetadata(null, OnItemCornerRadiusPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemCornerRadius attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemCornerRadius attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static CornerRadius? GetItemCornerRadius(ListViewBase element)
    {
        return (CornerRadius?)element.GetValue(ItemCornerRadiusProperty);
    }

    /// <summary>
    /// Sets the value of the ItemCornerRadius attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemCornerRadius(ListViewBase element, CornerRadius? value)
    {
        element.SetValue(ItemCornerRadiusProperty, value);
    }

    /// <summary>
    /// Identifies the attached dependency property that specifies the margin of the <see cref="ListViewBase"/> item.
    /// </summary>
    public static readonly DependencyProperty ItemMarginProperty = DependencyProperty.RegisterAttached(
        "ItemMargin",
        typeof(Thickness?),
        typeof(ListViewExtensions),
        new PropertyMetadata(null, OnItemMarginPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemMargin attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemMargin attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static Thickness? GetItemMargin(ListViewBase element)
    {
        return (Thickness?)element.GetValue(ItemMarginProperty);
    }

    /// <summary>
    /// Sets the value of the ItemMargin attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemMargin(ListViewBase element, Thickness? value)
    {
        element.SetValue(ItemMarginProperty, value);
    }

    /// <summary>
    /// Identifies the attached dependency property that specifies the minimum height of the <see cref="ListView"/> item.
    /// </summary>
    public static readonly DependencyProperty ItemMinHeightProperty = DependencyProperty.RegisterAttached(
        "ItemMinHeight",
        typeof(double?),
        typeof(ListViewExtensions),
        new PropertyMetadata(null, OnItemMinHeightPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemMinHeight attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemMinHeight attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static double? GetItemMinHeight(ListViewBase element)
    {
        return (double?)element.GetValue(ItemMinHeightProperty);
    }

    /// <summary>
    /// Sets the value of the ItemMinHeight attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemMinHeight(ListViewBase element, double? value)
    {
        element.SetValue(ItemMinHeightProperty, value);
    }

    /// <summary>
    /// Identifies the attached dependency property that specifies whether focus can be constrained to the items in a <see cref="ListViewBase"/>.
    /// </summary>
    public static readonly DependencyProperty ItemIsFocusEngagementEnabledProperty = DependencyProperty.RegisterAttached(
        "ItemIsFocusEngagementEnabled",
        typeof(bool?),
        typeof(ListViewExtensions),
        new PropertyMetadata(null, OnItemIsFocusEngagementEnabledPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemIsFocusEngagementEnabled attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemIsFocusEngagementEnabled attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static bool? GetItemIsFocusEngagementEnabled(ListViewBase element)
    {
        return (bool?)element.GetValue(ItemIsFocusEngagementEnabledProperty);
    }

    /// <summary>
    /// Sets the value of the ItemIsFocusEngagementEnabled attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemIsFocusEngagementEnabled(ListViewBase element, bool? value)
    {
        element.SetValue(ItemIsFocusEngagementEnabledProperty, value);
    }

    private static void OnListViewBaseUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemCornerRadiusOnContainerContentChanging;
            listViewBase.ContainerContentChanging -= ItemMarginOnContainerContentChanging;
            listViewBase.ContainerContentChanging -= ItemMinHeightOnContainerContentChanging;
            listViewBase.ContainerContentChanging -= ItemIsFocusEngagementEnabledOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;
        }
    }

    private static void OnItemCornerRadiusPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemCornerRadiusOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (GetItemCornerRadius(listViewBase) != null)
            {
                listViewBase.ContainerContentChanging += ItemCornerRadiusOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void OnItemMarginPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemMarginOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (GetItemMargin(listViewBase) != null)
            {
                listViewBase.ContainerContentChanging += ItemMarginOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void OnItemMinHeightPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemMinHeightOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (GetItemMinHeight(listViewBase) != null)
            {
                listViewBase.ContainerContentChanging += ItemMinHeightOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void OnItemIsFocusEngagementEnabledPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemIsFocusEngagementEnabledOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (GetItemIsFocusEngagementEnabled(listViewBase) != null)
            {
                listViewBase.ContainerContentChanging += ItemIsFocusEngagementEnabledOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }


    private static void ItemCornerRadiusOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue) return;
        var presenter = args.ItemContainer.FindDescendant<ListViewItemPresenter>();
        if (presenter != null && GetItemCornerRadius(sender) is { } cornerRadius)
        {
            presenter.CornerRadius = cornerRadius;
        }
    }

    private static void ItemMarginOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue || GetItemMargin(sender) is not { } margin) return;
        if (IsApiContract13Present)
        {
            // The ListViewItem in Windows 11 appears to have a Border element with a margin of '4,2,4,2',
            // likely intended to visually separate items while adhering to the minimum
            // touch target size guidelines of 40x40 effective pixels.
            //
            // If the selection mode is set to single, multiple, or extended the item's visual tree looks like this:
            //     ListViewItem
            //       Root [ListViewItemPresenter]
            //         Border (A border that specifies most of the layout and visual properties)
            //         Content
            //         Border (A selection indicator or CheckBox glyph)
            //
            //     GridViewItem
            //       Root [ListViewItemPresenter]
            //         Border (A border that specifies most of the layout and visual properties
            //         Content
            //         Border (The CheckBox glyph)
            //         Border (SelectedBorderBrush)
            //           Border (SelectedInnerBorderBrush)
            //
            // When selection is disabled, then the item's visual tree looks like this:
            //     ListViewItem
            //       Root [ListViewItemPresenter]
            //         Border (A border that specifies most of the layout and visual properties)
            //         Content
            var border = args.ItemContainer.FindDescendant<Border>();
            if (border != null)
            {
                border.Margin = margin;
            }
        }
        else
        {
            // Due to the absence of a Border element in the Windows 10 ListViewItem,
            // margin must be set at the container level. This introduces an inactive
            // hit-test region around the visual bounds of the item.
            args.ItemContainer.Margin = margin;
        }
    }

    private static void ItemMinHeightOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue || GetItemMinHeight(sender) is not { } minHeight) return;
        if (!IsApiContract13Present && GetItemMargin(sender) is { } margin)
        {
            double offsetMargin = margin.Top + margin.Bottom;
            double normalizedMinHeight = minHeight - offsetMargin;

            if (normalizedMinHeight >= 0)
            {
                // If a margin is applied to the container, we have to subtract the vertical values
                // from the minimum height to ensure it matches the Windows 11 ListViewItem dimensions.
                args.ItemContainer.MinHeight = normalizedMinHeight;
            }
        }
        else
        {
            args.ItemContainer.MinHeight = minHeight;
        }
    }

    private static void ItemIsFocusEngagementEnabledOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue || GetItemIsFocusEngagementEnabled(sender) is not { } isEnabled) return;
        args.ItemContainer.IsFocusEngagementEnabled = isEnabled;
    }
}

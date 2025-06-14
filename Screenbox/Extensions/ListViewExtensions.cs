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
    /// Identifies the attached dependency property that specifies the <see cref="CornerRadius"/> for the items in a <see cref="ListViewBase"/>.
    /// </summary>
    public static readonly DependencyProperty ItemCornerRadiusProperty = DependencyProperty.RegisterAttached(
        "ItemCornerRadius", typeof(CornerRadius), typeof(ListViewExtensions), new PropertyMetadata(null, OnItemCornerRadiusPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemCornerRadius attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemCornerRadius attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static CornerRadius GetItemCornerRadius(ListViewBase element)
    {
        return (CornerRadius)element.GetValue(ItemCornerRadiusProperty);
    }

    /// <summary>
    /// Sets the value of the ItemCornerRadius attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemCornerRadius(ListViewBase element, CornerRadius value)
    {
        element.SetValue(ItemCornerRadiusProperty, value);
    }

    /// <summary>
    /// Identifies the attached dependency property that specifies the margin between items in a <see cref="ListViewBase"/>.
    /// </summary>
    public static readonly DependencyProperty ItemMarginProperty = DependencyProperty.RegisterAttached(
        "ItemMargin", typeof(Thickness), typeof(ListViewExtensions), new PropertyMetadata(null, OnItemMarginPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemMargin attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemMargin attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static Thickness GetItemMargin(ListViewBase element)
    {
        return (Thickness)element.GetValue(ItemMarginProperty);
    }

    /// <summary>
    /// Sets the value of the ItemMargin attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemMargin(ListViewBase element, Thickness value)
    {
        element.SetValue(ItemMarginProperty, value);
    }

    /// <summary>
    /// Identifies the attached dependency property that specifies the minimum height for items in a <see cref="ListView"/>.
    /// </summary>
    public static readonly DependencyProperty ItemMinHeightProperty = DependencyProperty.RegisterAttached(
        "ItemMinHeight", typeof(double), typeof(ListViewExtensions), new PropertyMetadata(null, OnItemMinHeightPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemMinHeight attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemMinHeight attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static double GetItemMinHeight(ListViewBase element)
    {
        return (double)element.GetValue(ItemMinHeightProperty);
    }

    /// <summary>
    /// Sets the value of the ItemMinHeight attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemMinHeight(ListViewBase element, double value)
    {
        element.SetValue(ItemMinHeightProperty, value);
    }

    /// <summary>
    /// Identifies the attached dependency property that specifies whether focus can be constrained to the items in a <see cref="ListViewBase"/>.
    /// </summary>
    public static readonly DependencyProperty ItemIsFocusEngagementEnabledProperty = DependencyProperty.RegisterAttached(
        "ItemIsFocusEngagementEnabled", typeof(bool), typeof(ListViewExtensions), new PropertyMetadata(null, OnItemIsFocusEngagementEnabledPropertyChanged));

    /// <summary>
    /// Gets the value of the ItemIsFocusEngagementEnabled attached property from the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> from which the property value is read.</param>
    /// <returns>The current value of the ItemIsFocusEngagementEnabled attached property on the specified <see cref="ListViewBase"/>.</returns>
    public static bool GetItemIsFocusEngagementEnabled(ListViewBase element)
    {
        return (bool)element.GetValue(ItemIsFocusEngagementEnabledProperty);
    }

    /// <summary>
    /// Sets the value of the ItemIsFocusEngagementEnabled attached property on the specified <see cref="ListViewBase"/>.
    /// </summary>
    /// <param name="element">The <see cref="ListViewBase"/> on which to set the property value.</param>
    /// <param name="value">The new value to set the property to.</param>
    public static void SetItemIsFocusEngagementEnabled(ListViewBase element, bool value)
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

            if (ItemCornerRadiusProperty != null)
            {
                listViewBase.ContainerContentChanging += ItemCornerRadiusOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void ItemCornerRadiusOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue) return;
        var presenter = args.ItemContainer.FindDescendant<ListViewItemPresenter>();
        if (presenter != null)
        {
            presenter.CornerRadius = GetItemCornerRadius(sender);
        }
    }

    private static void OnItemMarginPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemMarginOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (ItemMarginProperty != null)
            {
                listViewBase.ContainerContentChanging += ItemMarginOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void ItemMarginOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue) return;
        if (!IsApiContract13Present)
        {
            // In Windows 10, the ListViewItemPresenter doesn't have an inner Border element,
            // resulting in the margin being applied at the ListViewItem container level.
            // This introduces an inactive hit-test region around the visual bounds of the item.
            args.ItemContainer.Margin = GetItemMargin(sender);
        }
        else
        {
            // The ListViewItemPresenter in Windows 11 appears to have an inner Border element,
            // likely intended to visually separate items while adhering to the minimum
            // touch target size guidelines of 40x40 effective pixels.
            //
            //  __________________________
            // | Border     2px           |
            // |      ______________      |
            // |     |     36px     |     |
            // | 4px |    Content   | 4px |
            // |     |______________|     |
            // |            2px           |
            // |__________________________|
            //
            var border = args.ItemContainer.FindDescendant<Border>();
            if (border != null)
            {
                border.Margin = GetItemMargin(sender);
            }
        }
    }

    private static void OnItemMinHeightPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemMinHeightOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (ItemMinHeightProperty != null)
            {
                listViewBase.ContainerContentChanging += ItemMinHeightOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void ItemMinHeightOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue) return;
        if (!IsApiContract13Present && ItemMarginProperty != null)
        {
            var margin = GetItemMargin(sender);
            double offsetMargin = margin.Top + margin.Bottom;

            // Adjustment required to accommodate the added item container margin.
            args.ItemContainer.MinHeight = GetItemMinHeight(sender) - offsetMargin;
        }
        else
        {
            args.ItemContainer.MinHeight = GetItemMinHeight(sender);
        }
    }

    private static void OnItemIsFocusEngagementEnabledPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemIsFocusEngagementEnabledOnContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (ItemIsFocusEngagementEnabledProperty != null)
            {
                listViewBase.ContainerContentChanging += ItemIsFocusEngagementEnabledOnContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void ItemIsFocusEngagementEnabledOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Phase > 0 || args.InRecycleQueue) return;
        args.ItemContainer.IsFocusEngagementEnabled = GetItemIsFocusEngagementEnabled(sender);
    }
}

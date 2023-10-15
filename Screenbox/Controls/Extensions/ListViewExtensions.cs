using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Controls.Extensions
{
    public static class ListViewExtensions
    {
        public static readonly DependencyProperty ItemCornerRadiusProperty = DependencyProperty.RegisterAttached(
            "ItemCornerRadius",
            typeof(CornerRadius),
            typeof(ListViewExtensions),
            new PropertyMetadata(default(CornerRadius), OnItemCornerRadiusChanged));

        public static readonly DependencyProperty ItemMarginProperty = DependencyProperty.RegisterAttached(
            "ItemMargin",
            typeof(Thickness),
            typeof(ListViewExtensions),
            new PropertyMetadata(default(Thickness), OnItemMarginChanged));

        public static void SetItemMargin(DependencyObject element, Thickness value)
        {
            element.SetValue(ItemMarginProperty, value);
        }

        public static Thickness GetItemMargin(DependencyObject element)
        {
            return (Thickness)element.GetValue(ItemMarginProperty);
        }

        public static void SetItemCornerRadius(DependencyObject element, CornerRadius value)
        {
            element.SetValue(ItemCornerRadiusProperty, value);
        }

        public static CornerRadius GetItemCornerRadius(DependencyObject element)
        {
            return (CornerRadius)element.GetValue(ItemCornerRadiusProperty);
        }

        private static void OnItemCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListViewBase listView) return;
            listView.ContainerContentChanging -= ChangeItemCornerRadius;
            listView.ContainerContentChanging += ChangeItemCornerRadius;
        }

        private static void OnItemMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListViewBase listView) return;
            listView.ContainerContentChanging -= ChangeItemMargin;
            listView.ContainerContentChanging += ChangeItemMargin;
        }

        private static void ChangeItemCornerRadius(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase > 0 || args.InRecycleQueue) return;
            CornerRadius cornerRadius = GetItemCornerRadius(sender);
            if (args.ItemContainer.FindDescendant<ListViewItemPresenter>() is { } presenter)
            {
                presenter.CornerRadius = cornerRadius;
            }
        }

        private static void ChangeItemMargin(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase > 0 || args.InRecycleQueue) return;
            Thickness margin = GetItemMargin(sender);
            if (args.ItemContainer.FindDescendant<Border>() is { } border)
            {
                border.Margin = margin;
            }
        }
    }
}

using CommunityToolkit.WinUI;
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
            new PropertyMetadata(default(CornerRadius), OnAttachedPropertyChanged));

        public static readonly DependencyProperty ItemMarginProperty = DependencyProperty.RegisterAttached(
            "ItemMargin",
            typeof(Thickness),
            typeof(ListViewExtensions),
            new PropertyMetadata(default(Thickness), OnAttachedPropertyChanged));

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

        private static void OnAttachedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListViewBase listView) return;
            listView.ContainerContentChanging -= ListViewOnContainerContentChanging;
            listView.ContainerContentChanging += ListViewOnContainerContentChanging;
        }

        private static void ListViewOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase > 0 || args.InRecycleQueue) return;
            CornerRadius cornerRadius = GetItemCornerRadius(sender);
            Thickness margin = GetItemMargin(sender);
            if (args.ItemContainer.FindDescendant<ListViewItemPresenter>() is { } presenter)
            {
                presenter.CornerRadius = cornerRadius;
            }

            if (args.ItemContainer.FindDescendant<Border>() is { } border)
            {
                border.Margin = margin;
            }
        }
    }
}

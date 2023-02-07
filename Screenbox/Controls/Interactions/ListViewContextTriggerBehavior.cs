#nullable enable

using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Xaml.Interactivity;

namespace Screenbox.Controls.Interactions
{
    internal class ListViewContextTriggerBehavior : Trigger<ListViewBase>
    {
        public event TypedEventHandler<ListViewContextTriggerBehavior, ListViewContextRequestedEventArgs>? ContextRequested;

        public static readonly DependencyProperty FlyoutProperty = DependencyProperty.Register(
            nameof(Flyout),
            typeof(FlyoutBase),
            typeof(ListViewContextTriggerBehavior),
            new PropertyMetadata(null));

        public FlyoutBase? Flyout
        {
            get => (FlyoutBase?)GetValue(FlyoutProperty);
            set => SetValue(FlyoutProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.ContextRequested += OnContextRequested;
            AssociatedObject.RightTapped += OnRightTapped;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.ContextRequested -= OnContextRequested;
            AssociatedObject.RightTapped -= OnRightTapped;
        }

        private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            if (args.OriginalSource is not SelectorItem element) return;
            ListViewContextRequestedEventArgs eventArgs = new(element);
            ContextRequested?.Invoke(this, eventArgs);
            if (eventArgs.Handled) return;

            Interaction.ExecuteActions(AssociatedObject, Actions, element.Content);
            if (Flyout == null) return;
            if (Flyout is MenuFlyout { Items: { } } menuFlyout)
            {
                SetMenuFlyoutDataContext(menuFlyout.Items, element.Content);
            }

            Flyout.ShowAt(element);
            args.Handled = true;
        }

        private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is not FrameworkElement element ||
                element.FindAscendantOrSelf<SelectorItem>() is not { } item) return;
            ListViewContextRequestedEventArgs eventArgs = new(item);
            ContextRequested?.Invoke(this, eventArgs);
            if (eventArgs.Handled) return;

            Interaction.ExecuteActions(AssociatedObject, Actions, element.DataContext);
            if (Flyout == null) return;
            if (Flyout is MenuFlyout { Items: { } } menuFlyout)
            {
                SetMenuFlyoutDataContext(menuFlyout.Items, element.DataContext);
                menuFlyout.ShowAt(element, e.GetPosition(element));
            }
            else
            {
                Flyout.ShowAt(element);
            }

            e.Handled = true;
        }

        private void SetMenuFlyoutDataContext(IList<MenuFlyoutItemBase> items, object? dataContext)
        {
            List<MenuFlyoutItemBase> menuFlyoutItems = new(items);
            for (int i = 0; i < menuFlyoutItems.Count; i++)
            {
                MenuFlyoutItemBase item = menuFlyoutItems[i];
                item.DataContext = dataContext;
                if (item is MenuFlyoutSubItem { Items: { } } subItem)
                {
                    menuFlyoutItems.AddRange(subItem.Items);
                }
            }
        }
    }
}

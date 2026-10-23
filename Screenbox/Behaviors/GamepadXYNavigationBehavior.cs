using Microsoft.Xaml.Interactivity;
using Screenbox.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;

namespace Screenbox.Behaviors;

/// <summary>
/// Behavior that modifies the default focus finding for a better gamepad navigation experience
/// </summary>
internal sealed partial class GamepadXYNavigationBehavior : Behavior<FrameworkElement>
{
    [DynamicWindowsRuntimeCast(typeof(NavigationView))]
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.KeyDown += AssociatedObject_OnKeyDown;

        if (AssociatedObject is NavigationView navView)
        {
            UpdateXYFocus(navView);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.KeyDown -= AssociatedObject_OnKeyDown;
    }

    [DynamicWindowsRuntimeCast(typeof(NavigationViewItem))]
    [DynamicWindowsRuntimeCast(typeof(Control))]
    private void AssociatedObject_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        FocusNavigationDirection direction = XboxHelper.GetNavigationDirection(e.OriginalKey);

        if (direction is FocusNavigationDirection.None)
            return;

        bool isNavView = FocusManager.GetFocusedElement() is NavigationViewItem;
        if (direction is FocusNavigationDirection.Left or FocusNavigationDirection.Right && isNavView)
            return;

        var options = new FindNextElementOptions()
        {
            SearchRoot = AssociatedObject,
            XYFocusNavigationStrategyOverride = XYFocusNavigationStrategyOverride.Projection,
        };

        var candidate = FocusManager.FindNextElement(direction, options);
        if (candidate is Control control)
        {
            e.Handled = control.Focus(FocusState.Keyboard);
        }
    }

    [DynamicWindowsRuntimeCast(typeof(Control))]
    [DynamicWindowsRuntimeCast(typeof(DependencyObject))]
    private static void UpdateXYFocus(NavigationView navView)
    {
        if (navView.PaneDisplayMode != NavigationViewPaneDisplayMode.Top) return;
        int count = navView.MenuItems.Count;
        for (int i = 0; i < count; i++)
        {
            if (navView.MenuItems[i] is not Control current) return;
            if (i < count - 1 && navView.MenuItems[i + 1] is DependencyObject next)
            {
                current.XYFocusRight = next;
            }

            if (i > 0 && navView.MenuItems[i - 1] is DependencyObject prev)
            {
                current.XYFocusLeft = prev;
            }

            if (i == count - 1 && navView.PaneFooter is Control footer)
            {
                current.XYFocusRight = footer;
                footer.XYFocusLeft = current;
            }
        }
    }
}

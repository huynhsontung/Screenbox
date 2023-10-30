using Microsoft.Xaml.Interactivity;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;

namespace Screenbox.Controls.Interactions;

/// <summary>
/// Behavior that modifies the default focus finding for a better gamepad navigation experience
/// </summary>
internal class GamepadXYNavigationBehavior : Behavior<FrameworkElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.KeyDown += AssociatedObjectOnKeyDown;
        if (AssociatedObject is NavigationView navView)
        {
            UpdateXYFocus(navView);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.KeyDown -= AssociatedObjectOnKeyDown;
    }

    private void AssociatedObjectOnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Exit if not a Gamepad event
        if (e.OriginalKey is not (>= VirtualKey.GamepadDPadUp and <= VirtualKey.GamepadDPadRight
            or >= VirtualKey.GamepadLeftThumbstickUp and <= VirtualKey.GamepadLeftThumbstickLeft))
            return;

        DependencyObject? candidate = null;
        FindNextElementOptions options = new()
        {
            SearchRoot = AssociatedObject,
            XYFocusNavigationStrategyOverride = XYFocusNavigationStrategyOverride.Projection
        };

        bool isNavView = FocusManager.GetFocusedElement() is NavigationViewItem;
        switch (e.Key)
        {
            case VirtualKey.Up:
                candidate = FocusManager.FindNextElement(FocusNavigationDirection.Up, options);
                break;
            case VirtualKey.Down:
                candidate = FocusManager.FindNextElement(FocusNavigationDirection.Down, options);
                break;
            case VirtualKey.Left when !isNavView:
                candidate = FocusManager.FindNextElement(FocusNavigationDirection.Left, options);
                break;
            case VirtualKey.Right when !isNavView:
                candidate = FocusManager.FindNextElement(FocusNavigationDirection.Right, options);
                break;
        }

        if (candidate is Control control)
        {
            e.Handled = control.Focus(FocusState.Keyboard);
        }
    }

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

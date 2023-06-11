using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;

namespace Screenbox.Controls.Interactions;

internal class NavViewXYFocusBehavior : Behavior<NavigationView>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject.PaneDisplayMode != NavigationViewPaneDisplayMode.Top) return;
        UpdateXYFocus();
    }

    private void UpdateXYFocus()
    {
        int count = AssociatedObject.MenuItems.Count;
        for (int i = 0; i < count; i++)
        {
            if (AssociatedObject.MenuItems[i] is not Control current) return;
            if (i < count - 1 && AssociatedObject.MenuItems[i + 1] is DependencyObject next)
            {
                current.XYFocusRight = next;
            }
            
            if (i > 0 && AssociatedObject.MenuItems[i - 1] is DependencyObject prev)
            {
                current.XYFocusLeft = prev;
            }

            if (i == count - 1 && AssociatedObject.PaneFooter is Control footer)
            {
                current.XYFocusRight = footer;
                footer.XYFocusLeft = current;
            }
        }
    }
}
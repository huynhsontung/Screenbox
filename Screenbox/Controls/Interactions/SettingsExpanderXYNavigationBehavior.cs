using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Controls.Interactions;

/// <summary>
/// A behavior that makes the <see cref="SettingsExpander"/> content accessible to XY focus navigation.
/// </summary>
internal class SettingsExpanderXYNavigationBehavior : Behavior<SettingsExpander>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += AssociatedObjectOnLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
    }

    private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is SettingsExpander settingsExpander)
        {
            var toggleButton = settingsExpander.FindDescendant<ToggleButton>(e => e.Name == "ExpanderHeader");
            if (toggleButton != null)
            {
                var contentPresenter = toggleButton.FindDescendant<ContentPresenter>(c => c.Name == "PART_ContentPresenter");
                if (contentPresenter != null)
                {
                    toggleButton.XYFocusRight = contentPresenter;

                    // Ensure that when using the D-pad or left stick to navigate left or right, focus will move to the associated settings expander
                    contentPresenter.XYFocusLeftNavigationStrategy = Windows.UI.Xaml.Input.XYFocusNavigationStrategy.NavigationDirectionDistance;
                    contentPresenter.XYFocusRightNavigationStrategy = Windows.UI.Xaml.Input.XYFocusNavigationStrategy.NavigationDirectionDistance;
                }
            }
        }
    }
}

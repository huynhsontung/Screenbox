using CommunityToolkit.WinUI;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Screenbox.Behaviors;

/// <summary>
/// A behavior that makes the <see cref="SplitButton"/> secondary button accessible to XY focus navigation.
/// </summary>
internal class SplitButtonXYNavigationBehavior : Behavior<SplitButton>
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
        if (sender is SplitButton splitButton)
        {
            // Ensure that when navigating to the right with D-pad/left stick, focus will move to the secondary button
            splitButton.XYFocusRightNavigationStrategy = Windows.UI.Xaml.Input.XYFocusNavigationStrategy.NavigationDirectionDistance;

            var secondaryButton = splitButton.FindDescendant<Button>(sb => sb.Name == "SecondaryButton");
            if (secondaryButton != null)
            {
                secondaryButton.IsTabStop = true;
                secondaryButton.XYFocusLeft = splitButton;

                // Adjust the button appearance to ensure its focus visual is consistent with the associated split button
                CornerRadius cornerRadius = splitButton.CornerRadius;
                secondaryButton.CornerRadius = new CornerRadius(0, cornerRadius.TopRight, cornerRadius.BottomRight, 0);
                secondaryButton.FocusVisualMargin = splitButton.FocusVisualMargin;
            }
        }
    }
}

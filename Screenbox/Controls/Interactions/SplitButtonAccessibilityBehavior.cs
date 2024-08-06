using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Screenbox.Controls.Interactions;
internal class SplitButtonAccessibilityBehavior : BehaviorBase<SplitButton>
{
    /// <summary>
    /// The dependency property for <see cref="PrimaryButtonTooltip"/>.
    /// </summary>
    public static readonly DependencyProperty PrimaryButtonTooltipProperty = DependencyProperty.Register(
        nameof(PrimaryButtonTooltip), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Get and set the <see cref="Microsoft.UI.Xaml.Controls.SplitButton"/> primary button tooltip.
    /// </summary>
    public string PrimaryButtonTooltip
    {
        get => (string)GetValue(PrimaryButtonTooltipProperty);
        set => SetValue(PrimaryButtonTooltipProperty, value);
    }

    /// <summary>
    /// The dependency property for <see cref="SecondaryButtonTooltip"/>.
    /// </summary>
    public static readonly DependencyProperty SecondaryButtonTooltipProperty = DependencyProperty.Register(
        nameof(SecondaryButtonTooltip), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Get and set the <see cref="Microsoft.UI.Xaml.Controls.SplitButton"/> secondary button tooltip.
    /// </summary>
    public string SecondaryButtonTooltip
    {
        get => (string)GetValue(SecondaryButtonTooltipProperty);
        set => SetValue(SecondaryButtonTooltipProperty, value);
    }

    /// <summary>
    /// The dependency property for <see cref="SecondaryButtonAccessKey"/>.
    /// </summary>
    public static readonly DependencyProperty SecondaryButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(SecondaryButtonAccessKey), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Get and set the <see cref="Microsoft.UI.Xaml.Controls.SplitButton"/> secondary button access key.
    /// </summary>
    public string SecondaryButtonAccessKey
    {
        get => (string)GetValue(SecondaryButtonAccessKeyProperty);
        set => SetValue(SecondaryButtonAccessKeyProperty, value);
    }

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        if (AssociatedObject.FindDescendant<Button>(pb => pb.Name == "PrimaryButton" ) is { } primaryButton)
        {
            ToolTipService.SetToolTip(primaryButton, PrimaryButtonTooltip);
        }

        if (AssociatedObject.FindDescendant<Button>(sb => sb.Name == "SecondaryButton") is { } secondaryButton)
        {
            secondaryButton.AccessKey = SecondaryButtonAccessKey;
            secondaryButton.ExitDisplayModeOnAccessKeyInvoked = false; // Don't dismiss the access key display
            secondaryButton.IsTabStop = true; // Make the button accessible to XY navigation
            ToolTipService.SetToolTip(secondaryButton, SecondaryButtonTooltip);
            AutomationProperties.SetAccessibilityView(secondaryButton, AccessibilityView.Content); // Expose the button to the UI Automation tree
            AutomationProperties.SetName(secondaryButton, SecondaryButtonTooltip); // Override the default automation name with the tooltip content
        }
    }
}

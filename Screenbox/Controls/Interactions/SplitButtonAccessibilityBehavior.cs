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
    public static readonly DependencyProperty PrimaryButtonTooltipProperty = DependencyProperty.Register(
        nameof(PrimaryButtonTooltip), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

    public string PrimaryButtonTooltip
    {
        get => (string)GetValue(PrimaryButtonTooltipProperty);
        set => SetValue(PrimaryButtonTooltipProperty, value);
    }

    public static readonly DependencyProperty SecondaryButtonTooltipProperty = DependencyProperty.Register(
        nameof(SecondaryButtonTooltip), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

    public string SecondaryButtonTooltip
    {
        get => (string)GetValue(SecondaryButtonTooltipProperty);
        set => SetValue(SecondaryButtonTooltipProperty, value);
    }

    public static readonly DependencyProperty SecondaryButtonAccessKeyProperty = DependencyProperty.Register(
        nameof(SecondaryButtonAccessKey), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

    public string SecondaryButtonAccessKey
    {
        get => (string)GetValue(SecondaryButtonAccessKeyProperty);
        set => SetValue(SecondaryButtonAccessKeyProperty, value);
    }

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        if (AssociatedObject.FindDescendant<Button>(btn => btn.Name == "PrimaryButton" ) is { } primaryButton)
        {
            ToolTipService.SetToolTip(primaryButton, PrimaryButtonTooltip);
        }

        if (AssociatedObject.FindDescendant<Button>(btn => btn.Name == "SecondaryButton") is { } secondaryButton)
        {
            secondaryButton.AccessKey = SecondaryButtonAccessKey;
            secondaryButton.ExitDisplayModeOnAccessKeyInvoked = false;
            secondaryButton.IsTabStop = true;
            ToolTipService.SetToolTip(secondaryButton, SecondaryButtonTooltip);
            AutomationProperties.SetAccessibilityView(secondaryButton, AccessibilityView.Content);
            AutomationProperties.SetName(secondaryButton, SecondaryButtonTooltip);
        }
    }
}

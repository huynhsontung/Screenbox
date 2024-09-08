using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls.Interactions
{
    /// <summary>
    /// This behavior asigns a <see cref="ToolTip"/> to each part of the <see cref="Microsoft.UI.Xaml.Controls.SplitButton"/> button.
    /// Enables navigation to the secondary part and don't dismiss the access key display when it's invoked.
    /// </summary>
    internal class SplitButtonAccessibilityBehavior : BehaviorBase<Microsoft.UI.Xaml.Controls.SplitButton>
    {
        /// <summary>
        /// Identifies the <see cref="PrimaryButtonTooltip"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonTooltipProperty = DependencyProperty.Register(
            nameof(PrimaryButtonTooltip), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the tooltip associated with the primary button.
        /// </summary>
        public string PrimaryButtonTooltip
        {
            get => (string)GetValue(PrimaryButtonTooltipProperty);
            set => SetValue(PrimaryButtonTooltipProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SecondaryButtonTooltip"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonTooltipProperty = DependencyProperty.Register(
            nameof(SecondaryButtonTooltip), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the tooltip associated with the secondary button.
        /// </summary>
        public string SecondaryButtonTooltip
        {
            get => (string)GetValue(SecondaryButtonTooltipProperty);
            set => SetValue(SecondaryButtonTooltipProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SecondaryButtonAccessKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonAccessKeyProperty = DependencyProperty.Register(
            nameof(SecondaryButtonAccessKey), typeof(string), typeof(SplitButtonAccessibilityBehavior), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the access key of the secondary button.
        /// </summary>
        public string SecondaryButtonAccessKey
        {
            get => (string)GetValue(SecondaryButtonAccessKeyProperty);
            set => SetValue(SecondaryButtonAccessKeyProperty, value);
        }

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();

            if (AssociatedObject.FindDescendant<Button>(pb => pb.Name == "PrimaryButton") is { } primaryButton)
            {
                ToolTipService.SetToolTip(primaryButton, PrimaryButtonTooltip);
            }

            if (AssociatedObject.FindDescendant<Button>(sb => sb.Name == "SecondaryButton") is { } secondaryButton)
            {
                secondaryButton.AccessKey = SecondaryButtonAccessKey;
                secondaryButton.ExitDisplayModeOnAccessKeyInvoked = false; // Don't dismiss the access key display
                secondaryButton.IsTabStop = true; // Include the button in gamepad/tab navigation
                ToolTipService.SetToolTip(secondaryButton, SecondaryButtonTooltip);
                AutomationProperties.SetAccessibilityView(secondaryButton, AccessibilityView.Content); // Expose the button to the UI Automation tree
                AutomationProperties.SetName(secondaryButton, SecondaryButtonTooltip); // Override the default automation name with the tooltip content
            }
        }
    }
}

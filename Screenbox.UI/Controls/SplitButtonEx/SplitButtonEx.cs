// Copyright (c) Tung Huynh and Contributors. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;

namespace Screenbox.UI.Controls;

/// <summary>
/// Represents an extended <see cref='SplitButton'/> control that provides a convenient way
/// to set independent access keys and tooltips for the primary and secondary button parts.
/// </summary>
/// <remarks>
/// You can modify the default XY focus navigation behavior using the <see cref="IsSecondaryButtonTabStop"/>
/// property to enable keyboard and game controller navigation to the secondary button part.
/// </remarks>
/// <example>
/// The following example shows how to create a <see cref="SplitButtonEx"/> with separate tool tips
/// for the primary and secondary button parts, and navigation to the secondary button part enabled.
/// <code lang="xml"><![CDATA[
/// <local:SplitButtonEx Flyout="{StaticResource BrushFlyout}"
///                      IsSecondaryButtonTabStop="True"
///                      PrimaryButtonToolTip="Foreground color">
///     <local:SplitButtonEx.SecondaryButtonToolTip>
///         <ToolTip Content="Color options" />
///     </local:SplitButtonEx.SecondaryButtonToolTip>
///     <Border x:Name="SelectedColorBorder" Width="20" Height="20"/>
/// </local:SplitButtonEx>
/// ]]></code>
/// </example>
public sealed partial class SplitButtonEx : SplitButton
{
    private const string PrimaryButtonName = "PrimaryButton";
    private const string SecondaryButtonName = "SecondaryButton";

    private readonly long _toolTipPlacementCallbackToken;

    private Button? _primaryButton;
    private Button? _secondaryButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitButtonEx"/> class.
    /// </summary>
    public SplitButtonEx()
    {
        DefaultStyleKey = typeof(SplitButton);

        _toolTipPlacementCallbackToken = RegisterPropertyChangedCallback(ToolTipService.PlacementProperty, OnToolTipServicePlacementChanged);
    }

    protected override void OnApplyTemplate()
    {
        _primaryButton = null;
        _secondaryButton = null;

        base.OnApplyTemplate();

        if (GetTemplateChild(PrimaryButtonName) is Button primaryButton)
        {
            _primaryButton = primaryButton;

            UpdatePrimaryButtonAccessKey();
            UpdatePrimaryButtonToolTip();
            UpdateToolTipPlacement(primaryButton);
        }

        if (GetTemplateChild(SecondaryButtonName) is Button secondaryButton)
        {
            _secondaryButton = secondaryButton;

            UpdateSecondaryButtonAccessKey();
            UpdateSecondaryButtonToolTip();
            UpdateToolTipPlacement(secondaryButton);
            UpdateIsSecondaryButtonTabStop();
        }
    }

    private void OnToolTipServicePlacementChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (_primaryButton is not null && PrimaryButtonToolTip is not null)
        {
            UpdateToolTipPlacement(_primaryButton);
        }

        if (_secondaryButton is not null && SecondaryButtonToolTip is not null)
        {
            UpdateToolTipPlacement(_secondaryButton);
        }
    }

    private void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        var property = e.Property;

        if (property == PrimaryButtonAccessKeyProperty)
        {
            UpdatePrimaryButtonAccessKey();
        }
        else if (property == PrimaryButtonToolTipProperty)
        {
            UpdatePrimaryButtonToolTip();
        }
        else if (property == SecondaryButtonAccessKeyProperty)
        {
            UpdateSecondaryButtonAccessKey();
        }
        else if (property == SecondaryButtonToolTipProperty)
        {
            UpdateSecondaryButtonToolTip();
        }
        else if (property == IsSecondaryButtonTabStopProperty)
        {
            UpdateIsSecondaryButtonTabStop();
        }
    }

    private void UpdatePrimaryButtonAccessKey()
    {
        string? accessKey = PrimaryButtonAccessKey;

        if (_primaryButton is null || string.IsNullOrEmpty(accessKey))
            return;

        _primaryButton.AccessKey = accessKey;
    }

    private void UpdatePrimaryButtonToolTip()
    {
        var primaryToolTip = PrimaryButtonToolTip;

        if (_primaryButton is null || primaryToolTip is null)
            return;

        ToolTipService.SetToolTip(_primaryButton, primaryToolTip);
    }

    private void UpdateSecondaryButtonAccessKey()
    {
        string? accessKey = SecondaryButtonAccessKey;

        if (_secondaryButton is null || string.IsNullOrEmpty(accessKey))
            return;

        _secondaryButton.AccessKey = accessKey;
        _secondaryButton.ExitDisplayModeOnAccessKeyInvoked = false;

        // Prevent the key tips from overlapping.
        if (KeyTipPlacementMode == KeyTipPlacementMode.Right
            && !string.IsNullOrEmpty(accessKey))
        {
            _secondaryButton.KeyTipPlacementMode = KeyTipPlacementMode.Auto;
        }
    }

    private void UpdateSecondaryButtonToolTip()
    {
        var secondaryToolTip = SecondaryButtonToolTip;

        if (_secondaryButton is null || secondaryToolTip is null)
            return;

        ToolTipService.SetToolTip(_secondaryButton, secondaryToolTip);

        switch (secondaryToolTip)
        {
            case string text:
                AutomationProperties.SetName(_secondaryButton, text);
                AutomationProperties.SetAccessibilityView(_secondaryButton, AccessibilityView.Content);
                break;
            case ToolTip toolTip when toolTip.Content is string content:
                AutomationProperties.SetName(_secondaryButton, content);
                AutomationProperties.SetAccessibilityView(_secondaryButton, AccessibilityView.Content);
                break;
        }
    }

    private void UpdateIsSecondaryButtonTabStop()
    {
        if (_secondaryButton is null || !IsSecondaryButtonTabStop)
            return;

        _secondaryButton.IsTabStop = true;
        _secondaryButton.XYFocusLeftNavigationStrategy = XYFocusNavigationStrategy.NavigationDirectionDistance;

        // Update the appearance of the secondary button part to match the focus visual of the control.
        var radius = CornerRadius;
        _secondaryButton.CornerRadius = new CornerRadius(0, radius.TopRight, radius.BottomRight, 0);
        _secondaryButton.FocusVisualMargin = FocusVisualMargin;
    }

    private void UpdateToolTipPlacement(Button button)
    {
        var placementMode = ToolTipService.GetPlacement(this);
        if (placementMode != PlacementMode.Top)
        {
            ToolTipService.SetPlacement(button, placementMode);
        }
    }
}

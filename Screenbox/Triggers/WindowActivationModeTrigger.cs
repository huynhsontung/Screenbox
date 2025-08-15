using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Screenbox.Triggers;

/// <summary>
/// Represents a declarative rule that applies visual states based on the <see cref="CoreWindow.ActivationMode"/> property.
/// </summary>
/// <remarks>
/// Use WindowActivationModeTriggers to create rules that automatically triggers a VisualState change when
/// the window is a specified activation state. When you use WindowActivationModeTriggers in your XAML markup,
/// you don't need to handle the <see cref="CoreWindow.Activated"/> event and call <see cref="VisualStateManager.GoToState"/> in your code.
/// </remarks>
/// <example>
/// This example shows how to use the <see cref="VisualState.StateTriggers"/> property with an <see cref="WindowActivationModeTrigger"/>
/// to create a declarative rule in XAML markup based on the activation state of the window.
/// <code lang="xaml">
/// &lt;Grid&gt;
///     &lt;StackPanel&gt;
///         &lt;TextBlock x:Name="FirstText"
///                    Foreground="{ThemeResource TextFillColorPrimaryBrush}"
///                    Text="This is a block of text. It is the 1st text block." /&gt;
///         &lt;TextBlock x:Name="LastText"
///                    Foreground="{ThemeResource TextFillColorPrimaryBrush}"
///                    Text="This is a block of text. It is the 2nd text block." /&gt;
///     &lt;/StackPanel&gt;
///     &lt;VisualStateManager.VisualStateGroups&gt;
///         &lt;VisualStateGroup&gt;
///             &lt;VisualState&gt;
///                 &lt;VisualState.StateTriggers&gt;
///                     &lt;!-- VisualState to be triggered when the activation state of the window is Deactivated. --&gt;
///                     &lt;local:WindowActivationModeTrigger ActivationMode="Deactivated" /&gt;
///                 &lt;/VisualState.StateTriggers&gt;
/// 
///                 &lt;VisualState.Setters&gt;
///                     &lt;Setter Target="FirstText.Foreground" Value="{ThemeResource TextFillColorDisabledBrush}" /&gt;
///                     &lt;Setter Target="LastText.Opacity" Value="0.4" /&gt;
///                 &lt;/VisualState.Setters&gt;
///             &lt;/VisualState&gt;
///         &lt;/VisualStateGroup&gt;
///     &lt;/VisualStateManager.VisualStateGroups&gt;
/// &lt;/Grid&gt;
/// </code>
/// </example>
[Windows.Foundation.Metadata.ContractVersion(typeof(Windows.Foundation.UniversalApiContract), 327680u)]
public sealed class WindowActivationModeTrigger : StateTriggerBase
{
    private readonly CoreWindow _coreWindow;

    /// <summary>
    /// Identifies the <see cref="ActivationMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ActivationModeProperty = DependencyProperty.Register(
        nameof(ActivationMode), typeof(CoreWindowActivationMode), typeof(WindowActivationModeTrigger), new PropertyMetadata(CoreWindowActivationMode.None, OnActivationModePropertyChanged));

    /// <summary>
    /// Gets or sets the activation mode that indicates whether the trigger should be applied.
    /// </summary>
    public CoreWindowActivationMode ActivationMode
    {
        get { return (CoreWindowActivationMode)GetValue(ActivationModeProperty); }
        set { SetValue(ActivationModeProperty, value); }
    }

    private static void OnActivationModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowActivationModeTrigger trigger)
        {
            trigger.UpdateTrigger();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowActivationModeTrigger"/> class,
    /// and registers a handler for when the window completes activation or deactivation.
    /// </summary>
    public WindowActivationModeTrigger()
    {
        _coreWindow = Window.Current?.CoreWindow;
        if (_coreWindow != null)
        {
            _coreWindow.Activated += CoreWindow_Activated;
        }
    }

    private void CoreWindow_Activated(CoreWindow sender, WindowActivatedEventArgs args)
    {
        UpdateTrigger();
    }

    private void UpdateTrigger()
    {
        if (_coreWindow != null)
        {
            SetActive(_coreWindow.ActivationMode == ActivationMode);
        }
        else
        {
            SetActive(false);
        }
    }
}

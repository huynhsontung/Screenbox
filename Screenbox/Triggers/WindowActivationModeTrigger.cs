#nullable enable

using CommunityToolkit.WinUI.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Screenbox.Triggers;

/// <summary>
/// Represents a declarative rule that applies visual states based on the
/// <see cref="CoreWindow.ActivationMode"/> property.
/// </summary>
/// <remarks>
/// Use WindowActivationModeTriggers to create rules that automatically triggers a
/// <see cref="VisualState"/> change when the window is a specified activation state.
/// When you use WindowActivationModeTriggers in your XAML markup, you don't need
/// to handle the <see cref="CoreWindow.Activated"/> event and call
/// <see cref="VisualStateManager.GoToState"/> in your code.
/// </remarks>
/// <example>
/// This example shows how to use the <see cref="VisualState.StateTriggers"/> property
/// with an <see cref="WindowActivationModeTrigger"/> to create a declarative rule in
/// XAML markup based on the activation state of the window.
/// <code lang="xml"><![CDATA[
/// <Page>
///     <Grid x:Name="LayoutRoot">
///         <TextBlock x:Name="ExampleText" Text="Hello World!" />
///         <VisualStateManager.VisualStateGroups>
///             <VisualStateGroup>
///                 <VisualState>
///                     <VisualState.StateTriggers>
///                         <!-- VisualState to be triggered when the activation state of the window is Deactivated. -->
///                         <local:WindowActivationModeTrigger ActivationMode="Deactivated" />
///                     </VisualState.StateTriggers>
///                     <VisualState.Setters>
///                         <Setter Target="LayoutRoot.Background" Value="Gray" />
///                         <Setter Target="ExampleText.Opacity" Value="0.4" />
///                     </VisualState.Setters>
///                 </VisualState>
///             </VisualStateGroup>
///         </VisualStateManager.VisualStateGroups>
///     </Grid>
/// </Page>
/// ]]></code>
/// </example>
[Windows.Foundation.Metadata.ContractVersion(typeof(Windows.Foundation.UniversalApiContract), 327680u)]
public sealed class WindowActivationModeTrigger : StateTriggerBase
{
    private readonly CoreWindow _coreWindow;
    private readonly WeakEventListener<WindowActivationModeTrigger, CoreWindow, WindowActivatedEventArgs> _weakEventListener;

    /// <summary>
    /// Identifies the <see cref="ActivationMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ActivationModeProperty = DependencyProperty.Register(
        nameof(ActivationMode),
        typeof(CoreWindowActivationMode),
        typeof(WindowActivationModeTrigger),
        new PropertyMetadata(CoreWindowActivationMode.None, (d, e) => ((WindowActivationModeTrigger)d).UpdateTrigger()));

    /// <summary>
    /// Gets or sets the activation mode at which the <see cref="VisualState"/>
    /// should be applied.
    /// </summary>
    /// <value>A value that indicates when the <see cref="VisualState"/> should
    /// be applied. The default is <b>None</b>.</value>
    public CoreWindowActivationMode ActivationMode
    {
        get { return (CoreWindowActivationMode)GetValue(ActivationModeProperty); }
        set { SetValue(ActivationModeProperty, value); }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowActivationModeTrigger"/> class.
    /// </summary>
    public WindowActivationModeTrigger()
    {
        _coreWindow = Window.Current.CoreWindow;

        _weakEventListener = new(this)
        {
            OnEventAction = static (instance, sender, args) => instance.CoreWindow_OnActivated(sender, args),
            OnDetachAction = (weakEventListener) => _coreWindow.Activated -= weakEventListener.OnEvent
        };

        _coreWindow.Activated += _weakEventListener.OnEvent;
        UpdateTrigger();
    }

    private void CoreWindow_OnActivated(CoreWindow sender, WindowActivatedEventArgs args)
    {
        UpdateTrigger();
    }

    private void UpdateTrigger()
    {
        SetActive(_coreWindow?.ActivationMode == ActivationMode);
    }
}

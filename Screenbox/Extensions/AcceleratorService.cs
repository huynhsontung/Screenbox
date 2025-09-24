#nullable enable

using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Extensions;

/// <summary>
/// Represents a service that provides <see langword="static"/> methods to display a <see cref="ToolTip"/>,
/// with the corresponding key combinations appended at the end.
/// </summary>
/// <remarks>If a control has more than one accelerator defined, only the first is presented.</remarks>
/// <example>
/// In this example, we specify the ToolTip for a Button. The keyboard accelerator is
/// displayed in the UI element flyout as "Create a new document (Ctrl+Alt+N)".
/// <code lang="xml">
/// &lt;Button Content="New" local:AcceleratorService.ToolTip="Create a new document"&gt;
///    &lt;Button.KeyboardAccelerators&gt;
///        &lt;KeyboardAccelerator Key="N" Modifiers="Control,Menu" /&gt;
///    &lt;/Button.KeyboardAccelerators&gt;
/// &lt;/Button&gt;
/// </code>
/// or
/// <code lang="csharp">
/// var button = new Button { Content = "New" };
/// AcceleratorService.SetToolTip(button, "Create a new document");
/// button.KeyboardAccelerators.Add(new KeyboardAccelerator
/// {
///     Key = VirtualKey.N,
///     Modifiers = KeyboardAcceleratorModifiers.Control | KeyboardAcceleratorModifiers.Menu
/// });
/// </code>
/// </example>
[Windows.Foundation.Metadata.ContractVersion(typeof(Windows.Foundation.UniversalApiContract), 327680u)]
public sealed class AcceleratorService
{
    /// <summary>
    /// Identifies the AcceleratorService.ToolTip XAML attached property.
    /// </summary>
    public static readonly DependencyProperty ToolTipProperty = DependencyProperty.RegisterAttached(
        "ToolTip", typeof(string), typeof(AcceleratorService), new PropertyMetadata(string.Empty, OnToolTipPropertyChanged));

    /// <summary>
    /// Gets the value of the AcceleratorService.ToolTip XAML attached property for a <see cref="UIElement"/>.
    /// </summary>
    /// <param name="element">The element from which the property value is read.</param>
    /// <returns>The string tooltip content.</returns>
    public static string GetToolTip(UIElement element)
    {
        return (string)element.GetValue(ToolTipProperty);
    }

    /// <summary>
    /// Sets the value of the AcceleratorService.ToolTip XAML attached property.
    /// </summary>
    /// <param name="element">The element to set tooltip content on.</param>
    /// <param name="value">The string to set as tooltip content.</param>
    public static void SetToolTip(UIElement element, string value)
    {
        element.SetValue(ToolTipProperty, value);
    }

    private static void OnToolTipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && e.NewValue is string value)
        {
            string toolTipString = value;

            if (DeviceInfoHelper.IsKeyboardPresent)
            {
                var keyboardAccelerators = element.KeyboardAccelerators;
                if (keyboardAccelerators.Count > 0)
                {
                    var keyboardAccelerator = keyboardAccelerators[0];
                    if (keyboardAccelerator.IsEnabled)
                    {
                        string keyboardAcceleratorText = keyboardAccelerator.ToDisplayName();
                        if (!string.IsNullOrEmpty(keyboardAcceleratorText))
                        {
                            toolTipString = GlobalizationHelper.IsRightToLeftLanguage
                                ? $"({keyboardAcceleratorText}) {value}"
                                : $"{value} ({keyboardAcceleratorText})";
                        }
                    }
                }
            }

            ToolTipService.SetToolTip(element, toolTipString);
        }
    }
}

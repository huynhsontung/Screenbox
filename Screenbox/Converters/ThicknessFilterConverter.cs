// Source (inspired by and ported to C#): https://github.com/microsoft/microsoft-ui-xaml/pull/6829

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Defines constants that specify the filter type for a <see cref="ThicknessFilterConverter"/> instance.
/// <para>This enumeration supports a bitwise combination of its member values.</para>
/// </summary>
/// <remarks>This enumeration is used by the <see cref="ThicknessFilterConverter.Filter"/> property.</remarks>
[Flags]
public enum ThicknessFilterKinds
{
    /// <summary>
    /// No filter applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Filters Left value, sets Top, Right and Bottom to 0.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Filters Top value, sets Left, Right and Bottom to 0.
    /// </summary>
    Top = 2,

    /// <summary>
    /// Filters Right value, sets Left, Top and Bottom to 0.
    /// </summary>
    Right = 4,

    /// <summary>
    /// Filters Bottom value, sets Left, Top and Right to 0.
    /// </summary>
    Bottom = 8,
}

/// <summary>
/// Converts an existing <see cref="Thickness"/> struct to a new <see cref="Thickness"/> struct,
/// with filters applied to extract only the specified fields, leaving the others set to 0.
/// </summary>
/// <remarks>
/// Use the <see cref="ThicknessFilterConverter"/> with a Binding/x:Bind or TemplateBinding
/// to create a new <see cref="Thickness"/> struct from an existing one.
/// </remarks>
/// <example>
/// The following example shows how to use the <see cref="ThicknessFilterConverter"/> element.
/// <code lang="xaml">
/// &lt;ControlTemplate TargetType="Button"&gt;
///     &lt;Grid&gt;
///         &lt;Grid.Resources&gt;
///             &lt;local:ThicknessFilterConverter x:Name="VerticalThicknessFilterConverter" Filter="Top,Bottom" /&gt;
///         &lt;/Grid.Resources&gt;
///         &lt;Border Background="{TemplateBinding Background}"
///                 BorderBrush="{TemplateBinding BorderBrush}"
///                 BorderThickness="{Binding BorderThickness, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource VerticalThicknessFilterConverter}}"
///                 Padding="{TemplateBinding Padding}" /&gt;
///     &lt;/Grid&gt;
/// &lt;/ControlTemplate&gt;
/// </code>
/// <code lang="xaml">
/// &lt;Grid&gt;
///     &lt;Grid.Resources&gt;
///         &lt;local:ThicknessFilterConverter x:Name="VerticalThicknessFilterConverter" Filter="Top,Bottom" /&gt;
///         &lt;Thickness x:Key="ExampleBorderThickness"&gt;1,1,1,1&lt;/Thickness&gt;
///     &lt;/Grid.Resources&gt;
///     &lt;Border Background="DarkBlue"
///             BorderBrush="Cyan"
///             BorderThickness="{Binding Source={StaticResource ExampleBorderThickness}, Converter={StaticResource VerticalThicknessFilterConverter}}" /&gt;
/// &lt;/Grid&gt;
/// </code>
/// <code lang="cs">
/// var myBorder = new Border();
/// var exampleThickness = new Thickness(1, 1, 1, 1);
/// 
/// // Create the converter instance and the filter type.
/// var thicknessConverter = new ThicknessFilterConverter();
/// var thicknessFilter = ThicknessFilterKinds.Top | ThicknessFilterKinds.Bottom;
///
/// // Attach the converter to the target. For example:
/// myBorder.BorderThickness = thicknessConverter.Convert(exampleThickness, thicknessFilter);
/// </code>
/// </example>
public sealed partial class ThicknessFilterConverter : DependencyObject, IValueConverter
{
    /// <summary>
    /// Identifies the <see cref="Filter"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
        nameof(Filter), typeof(ThicknessFilterKinds), typeof(ThicknessFilterConverter), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the type of the filter applied to the <see cref="ThicknessFilterConverter"/>.
    /// </summary>
    public ThicknessFilterKinds Filter
    {
        get { return (ThicknessFilterKinds)GetValue(FilterProperty); }
        set { SetValue(FilterProperty, value); }
    }

    /// <summary>
    /// Extracts the specified fields from a <see cref="Thickness"/> struct.
    /// </summary>
    /// <param name="thickness">The source <see cref="Thickness"/> to convert.</param>
    /// <param name="filterKind">An enumeration that specifies the filter type.</param>
    /// <returns>A <see cref="Thickness"/> with only the fields specified by the filter, while the rest are set to 0.</returns>
    public Thickness Convert(Thickness thickness, ThicknessFilterKinds filterKind)
    {
        var result = thickness;

        switch (filterKind)
        {
            case ThicknessFilterKinds.None:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Left:
                //result.Left = thickness.Left;
                result.Top = 0;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKinds.Top:
                result.Left = 0;
                //result.Top = thickness.Top;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKinds.Left | ThicknessFilterKinds.Top:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKinds.Right:
                result.Left = 0;
                result.Top = 0;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKinds.Left | ThicknessFilterKinds.Right:
                //result.Left = thickness.Left;
                result.Top = 0;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKinds.Top | ThicknessFilterKinds.Right:
                result.Left = 0;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKinds.Left | ThicknessFilterKinds.Top | ThicknessFilterKinds.Right:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKinds.Bottom:
                result.Left = 0;
                result.Top = 0;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Left | ThicknessFilterKinds.Bottom:
                //result.Left = thickness.Left;
                result.Top = 0;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Top | ThicknessFilterKinds.Bottom:
                result.Left = 0;
                //result.Top = thickness.Top;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Left | ThicknessFilterKinds.Top | ThicknessFilterKinds.Bottom:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Right | ThicknessFilterKinds.Bottom:
                result.Left = 0;
                result.Top = 0;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Left | ThicknessFilterKinds.Right | ThicknessFilterKinds.Bottom:
                //result.Left = thickness.Left;
                result.Top = 0;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Top | ThicknessFilterKinds.Right | ThicknessFilterKinds.Bottom:
                result.Left = 0;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKinds.Left | ThicknessFilterKinds.Top | ThicknessFilterKinds.Right | ThicknessFilterKinds.Bottom:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
        }

        return result;
    }

    /// <summary>
    /// Converts the source <see cref="Thickness"/> by extracting only the fields specified by the <see cref="Filter"/> and setting the others to 0.
    /// </summary>
    /// <param name="value">The source <see cref="Thickness"/> being passed to the target.</param>
    /// <param name="targetType">The type of the target property. Not used.</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic. Not used.</param>
    /// <param name="language">The language of the conversion. Not used.</param>
    /// <returns>The converted <see cref="Thickness"/> value to be passed to the target dependency property.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Thickness thickness)
        {
            var filterType = Filter;
            return Convert(thickness, filterType);
        }

        return value;
    }

    /// <summary>
    /// Not implemented.
    /// </summary>
    /// <param name="value">The target data being passed to the source.</param>
    /// <param name="targetType">The type of the target property.</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
    /// <param name="language">The language of the conversion.</param>
    /// <returns>The value to be passed to the source object.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

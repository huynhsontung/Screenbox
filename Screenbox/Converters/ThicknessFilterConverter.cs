// Source (Inspired by and ported to C#): https://github.com/microsoft/microsoft-ui-xaml/pull/6829

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// An <see cref="IValueConverter"/> that takes an existing <see cref="Thickness"/> struct and returns a new one,
/// using filters to keep only the specified fields and setting all others to 0.
/// <example>For example:
/// <code lang="xaml">
/// &lt;ControlTemplate TargetType="Button"&gt;
///     &lt;Grid&gt;
///         &lt;Grid.Resources&gt;
///             &lt;local:ThicknessFilterConverter x:Name="TopThicknessFilterConverter" Filter="Top" /&gt;
///         &lt;/Grid.Resources&gt;
///         &lt;Border Background="{TemplateBinding Background}"
///                 BorderThickness="{Binding BorderThickness, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource TopThicknessFilterConverter}}"
///                 Padding="{TemplateBinding Padding}" /&gt;
///     &lt;/Grid&gt;
/// &lt;/ControlTemplate&gt;
/// 
/// &lt;Grid&gt;
///     &lt;Grid.Resources&gt;
///         &lt;local:ThicknessFilterConverter x:Name="TopThicknessFilterConverter" Filter="Top" /&gt;
///         &lt;Thickness x:Key="ExampleBorderThickness"&gt;1,1,1,1&lt;/Thickness&gt;
///     &lt;/Grid.Resources&gt;
///     &lt;Border Background="Blue"
///             BorderThickness="{Binding Source={StaticResource ExampleBorderThickness}, Converter={StaticResource TopThicknessFilterConverter}}" /&gt;
/// &lt;/Grid&gt;
/// </code>
/// results in the BorderThickness of the <c>Border</c> having the value "0,1,0,0".
/// </example>
/// </summary>
public sealed class ThicknessFilterConverter : DependencyObject, IValueConverter
{
    /// <summary>
    /// Specifies the filter type used in a <see cref="ThicknessFilterConverter"/> instance.
    /// </summary>
    public enum ThicknessFilterKind
    {
        /// <summary>
        /// No filter applied.
        /// </summary>
        None,

        /// <summary>
        /// Filters Left value, sets Top, Right and Bottom to 0.
        /// </summary>
        Left,

        /// <summary>
        /// Filters Top value, sets Left, Right and Bottom to 0.
        /// </summary>
        Top,

        /// <summary>
        /// Filters Right value, sets Left, Top, and Bottom to 0.
        /// </summary>
        Right,

        /// <summary>
        /// Filters Bottom value, sets Left, Top, and Right to 0.
        /// </summary>
        Bottom,

        /// <summary>
        /// Filters Left and Right values, sets Top and Bottom to 0.
        /// </summary>
        LeftRight,

        /// <summary>
        /// Filters Top and Bottom values, sets Left and Right to 0.
        /// </summary>
        TopBottom,

        /// <summary>
        /// Filters Left and Top values, sets Right and Bottom to 0.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Filters Top and Right values, sets Left and Bottom to 0.
        /// </summary>
        TopRight,

        /// <summary>
        /// Filters Left and Bottom values, sets Top and Right to 0.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Filters Bottom and Right values, sets Left and Top to 0.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Filters Left, Top, and Right values, set Bottom to 0.
        /// </summary>
        LeftRightTop,

        /// <summary>
        /// Filters Left, Right, and Bottom values, set Top to 0.
        /// </summary>
        LeftRightBottom,

        /// <summary>
        /// Filters Left, Top, and Bottom values, set Right to 0.
        /// </summary>
        TopBottomLeft,

        /// <summary>
        /// Filters Top, Right, and Bottom values, set Left to 0.
        /// </summary>
        TopBottomRight
    }

    /// <summary>
    /// Identifies the <see cref="Filter"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
        nameof(Filter), typeof(ThicknessFilterKind), typeof(ThicknessFilterConverter), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the type of the filter applied to the <see cref="ThicknessFilterConverter"/>.
    /// </summary>
    public ThicknessFilterKind Filter
    {
        get { return (ThicknessFilterKind)GetValue(FilterProperty); }
        set { SetValue(FilterProperty, value); }
    }

    /// <summary>
    /// Extracts, using filters, the specified fields from a <see cref="Thickness"/> struct.
    /// </summary>
    /// <param name="thickness">The <see cref="Thickness"/> to convert.</param>
    /// <param name="filterKind">An <see cref="enum"/> that defines the filter type.</param>
    /// <returns>A <see cref="Thickness"/> with only the fields specified by the filter, while the rest are set to 0.</returns>
    public Thickness ExtractThickness(Thickness thickness, ThicknessFilterKind filterKind)
    {
        Thickness result = thickness;

        switch (filterKind)
        {
            case ThicknessFilterKind.Left:
                //result.Left = thickness.Left;
                result.Top = 0;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.Top:
                result.Left = 0;
                //result.Top = thickness.Top;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.Right:
                result.Left = 0;
                result.Top = 0;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.Bottom:
                result.Left = 0;
                result.Top = 0;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.LeftRight:
                //result.Left = thickness.Left;
                result.Top = 0;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.TopBottom:
                result.Left = 0;
                //result.Top = thickness.Top;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.TopLeft:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.TopRight:
                result.Left = 0;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.BottomLeft:
                //result.Left = thickness.Left;
                result.Top = 0;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.BottomRight:
                result.Left = 0;
                result.Top = 0;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.LeftRightTop:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.LeftRightBottom:
                //result.Left = thickness.Left;
                result.Top = 0;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.TopBottomLeft:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.TopBottomRight:
                result.Left = 0;
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
            return ExtractThickness(thickness, Filter);
        }
        else
        {
            return value;
        }
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

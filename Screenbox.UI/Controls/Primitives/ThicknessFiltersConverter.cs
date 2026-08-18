// Inspired by:
// https://github.com/microsoft/microsoft-ui-xaml/pull/6829

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.UI.Controls.Primitives;

/// <summary>
/// Converts an existing <see cref="Thickness"/> struct to a new <see cref="Thickness"/> struct,
/// with filters applied to extract only the specified fields, leaving the others set to 0.
/// </summary>
/// <remarks>
/// Use the <see cref="ThicknessFiltersConverter"/> with a Binding, x:Bind or TemplateBinding
/// to create a new <see cref="Thickness"/> struct from an existing one.
/// </remarks>
/// <example>
/// The following example shows how to use the <see cref="ThicknessFiltersConverter"/> element.
/// <code lang="xml"><![CDATA[
/// <ControlTemplate TargetType="Button">
///     <Grid>
///         <Grid.Resources>
///             <local:ThicknessFiltersConverter x:Name="VerticalThicknessFiltersConverter" Filters="Top,Bottom" />
///         </Grid.Resources>
///         <Border Background="{TemplateBinding Background}"
///                 BorderBrush="{TemplateBinding BorderBrush}"
///                 BorderThickness="{Binding BorderThickness, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource VerticalThicknessFiltersConverter}}"
///                 Padding="{TemplateBinding Padding}" />
///     </Grid>
/// </ControlTemplate>
/// ]]></code>
/// <code lang="xml"><![CDATA[
/// <Grid>
///     <Grid.Resources>
///         <local:ThicknessFiltersConverter x:Name="VerticalThicknessFiltersConverter" Filters="Top,Bottom" />
///         <Thickness x:Key="ExampleBorderThickness">1,1,1,1</Thickness>
///     </Grid.Resources>
///     <Border Background="DarkBlue"
///             BorderBrush="Cyan"
///             BorderThickness="{Binding Source={StaticResource ExampleBorderThickness}, Converter={StaticResource VerticalThicknessFiltersConverter}}" />
/// </Grid>
/// ]]></code>
/// <code lang="c#">
/// var myBorder = new Windows.UI.Xaml.Controls.Border();
/// var exampleThickness = new Windows.UI.Xaml.Thickness(1, 1, 1, 1);
/// 
/// // Create the converter instance and the filter type.
/// var thicknessConverter = new ThicknessFiltersConverter();
/// var thicknessFilter = ThicknessFilterKinds.Top | ThicknessFilterKinds.Bottom;
///
/// // Attach the converter to the target. For example:
/// myBorder.BorderThickness = thicknessConverter.Extract(exampleThickness, thicknessFilter);
/// </code>
/// </example>
public sealed partial class ThicknessFiltersConverter : DependencyObject, IValueConverter
{
    /// <summary>
    /// Identifies the <see cref="Filters"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FiltersProperty = DependencyProperty.Register(
        nameof(Filters),
        typeof(ThicknessFilterKinds),
        typeof(ThicknessFiltersConverter),
        new PropertyMetadata(ThicknessFilterKinds.None));

    /// <summary>
    /// Gets or sets the type of the filter applied to the <see cref="ThicknessFiltersConverter"/>.
    /// </summary>
    public ThicknessFilterKinds Filters
    {
        get { return (ThicknessFilterKinds)GetValue(FiltersProperty); }
        set { SetValue(FiltersProperty, value); }
    }

    /// <summary>
    /// Creates a new <see cref="Thickness"/> containing only the components specified
    /// by the provided <see cref="ThicknessFilterKinds"/> combination.
    /// </summary>
    /// <param name="thickness">The source <see cref="Thickness"/> from which component values are extracted.</param>
    /// <param name="filterKinds">A bitwise combination of the enumeration values that specifies which components of the <see cref="Thickness"/> to extract.</param>
    /// <returns>
    /// A <see cref="Thickness"/> containing only the specified fields from <paramref name="thickness"/>,
    /// the others are set to 0.
    /// </returns>
    public static Thickness Extract(Thickness thickness, ThicknessFilterKinds filterKinds)
    {
        if (filterKinds != ThicknessFilterKinds.None)
        {
            return new Thickness(
                filterKinds.HasFlag(ThicknessFilterKinds.Left) ? thickness.Left : 0,
                filterKinds.HasFlag(ThicknessFilterKinds.Top) ? thickness.Top : 0,
                filterKinds.HasFlag(ThicknessFilterKinds.Right) ? thickness.Right : 0,
                filterKinds.HasFlag(ThicknessFilterKinds.Bottom) ? thickness.Bottom : 0);
        }

        return new Thickness();
    }

    /// <summary>
    /// Converts the source <see cref="Thickness"/> by extracting only the fields specified
    /// by the <see cref="Filters"/> and setting the others to 0.
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
            return Extract(thickness, Filters);
        }

        return DependencyProperty.UnsetValue;
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

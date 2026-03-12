#nullable enable

using System;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Converts a numeric value to a formatted string with a specified unit appended.
/// </summary>
/// <remarks>
/// Use the <see cref="UnitDisplayConverter"/> to format a <see cref="double"/> value
/// according to the <see cref="DecimalPlaces"/> and <see cref="TrimTrailingZeros"/> properties,
/// and optionally appends the specified <see cref="Unit"/>.
/// </remarks>
/// <example>
/// The following example shows how to use the <see cref="UnitDisplayConverter"/> element.
/// <code lang="xml"><![CDATA[
/// <Page.Resources>
///     <local:UnitDisplayConverter x:Key="WeightUnitDisplayConverter"
///                                 Unit="Kg"
///                                 DecimalPlaces="3"
///                                 TrimTrailingZeros="False" />
/// </Page.Resources>
///
/// <Grid>
///     <!-- For example, when the value is '1.250002', the tool tip displays '1.250 Kg'. -->
///     <Slider ThumbToolTipValueConverter="{StaticResource WeightUnitDisplayConverter}" Value="{x:Bind Weight, Mode=TwoWay}" />
/// </Grid>    
/// ]]></code>
/// </example>
public sealed class UnitDisplayConverter : DependencyObject, IValueConverter
{
    private static readonly CultureInfo _cultureInfo = CultureInfo.CurrentCulture;

    /// <summary>
    /// Identifies the <see cref="Unit"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(UnitDisplayConverter), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the unit string appended to the formatted value.
    /// </summary>
    public string Unit
    {
        get { return (string)GetValue(UnitProperty); }
        set { SetValue(UnitProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="DecimalPlaces"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
        nameof(DecimalPlaces), typeof(int), typeof(UnitDisplayConverter), new PropertyMetadata(1));

    /// <summary>
    /// Gets or sets the minimum number of digits to display for the fraction
    /// part of the number.
    /// </summary>
    public int DecimalPlaces
    {
        get { return (int)GetValue(DecimalPlacesProperty); }
        set { SetValue(DecimalPlacesProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="TrimTrailingZeros"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TrimTrailingZerosProperty = DependencyProperty.Register(
        nameof(TrimTrailingZeros), typeof(bool), typeof(UnitDisplayConverter), new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets a value indicating whether trailing zeros are trimmed
    /// from the fractional part.
    /// </summary>
    public bool TrimTrailingZeros
    {
        get { return (bool)GetValue(TrimTrailingZerosProperty); }
        set { SetValue(TrimTrailingZerosProperty, value); }
    }

    /// <summary>
    /// Formats a <see cref="double"/> value to a string using the specified number of
    /// decimal places and optional trimming of trailing zeros, and appends the provided
    /// unit.
    /// </summary>
    /// <param name="unit">The unit string to append to the formatted value.</param>
    /// <param name="value">The numeric value to format.</param>
    /// <param name="fractionDigits">The number of fractional digits to include.</param>
    /// <param name="trimTrailingZeros">If <see langword="true"/>, trailing zeros in the fractional part are removed.</param>
    /// <returns>A string representation of the value and with the unit appended.</returns>
    public static string FormatDouble(string unit, double value, int fractionDigits, bool trimTrailingZeros, IFormatProvider? provider = null)
    {
        // Limit fractional digits to a range of 0–4 to align with the internal slider behavior,
        // since we cannot directly access the StepFrequency property.
        // https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.8.5/src/dxaml/xcp/dxaml/lib/Slider_Partial.cpp#L1847-L1936
        int digits = Math.Clamp(fractionDigits, 0, 4);

        string format = digits <= 0
            ? "0"
            : $"0.{new string(!trimTrailingZeros ? '0' : '#', digits)}";

        double roundedValue = Math.Round(value, digits);

        return string.IsNullOrWhiteSpace(unit)
            ? roundedValue.ToString(format, provider)
            : $"{roundedValue.ToString(format, provider)} {unit}";
    }

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            double d => FormatDouble(Unit, d, DecimalPlaces, TrimTrailingZeros, _cultureInfo),
            _ => DependencyProperty.UnsetValue
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

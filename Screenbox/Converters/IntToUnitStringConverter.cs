#nullable enable

using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Defines the unit types for formatting numeric values with localized and pluralized resource strings.
/// </summary>
/// <remarks>This enumeration is used by the <see cref="IntToUnitStringConverter.Unit"/> property.</remarks>
public enum UnitType
{
    None = 0,
    Albums = 1,
    Songs = 2,
    Seconds = 4,
    Items = 11,
}

/// <summary>
/// Converts an integer representing a quantity into a localized string representation for a specified unit,
/// automatically adjusting for singular or plural forms based on the value.
/// </summary>
public sealed class IntToUnitStringConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the unit type used to represent the count.
    /// </summary>
    public UnitType Unit { get; set; } = UnitType.None;

    /// <summary>
    /// Converts an <see cref="int"/> value to a localized <see cref="string"/>,
    /// using the appropriate singular or plural resource for the specified unit type.
    /// </summary>
    /// <param name="value">The <see cref="int"/> being passed to the target.</param>
    /// <param name="targetType">The type of the target property. Not used.</param>
    /// <param name="parameter">An optional parameter to be used in the converter logic. Not used.</param>
    /// <param name="language">The language of the conversion. Not used.</param>
    /// <returns>The <see cref="string"/> representing the amount and its unit; otherwise, the value as a <see cref="string"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int quantity && Unit is not UnitType.None)
        {
            return GetLocalizedCountAndUnit(quantity, Unit);
        }

        return value?.ToString() ?? string.Empty;
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

    private string GetLocalizedCountAndUnit(int value, UnitType unit)
    {
        return unit switch
        {
            UnitType.Albums => Strings.Resources.AlbumsCount(value),
            UnitType.Songs => Strings.Resources.SongsCount(value),
            UnitType.Seconds => Strings.Resources.SecondsCount(value),
            UnitType.Items => Strings.Resources.ItemsCount(value),
            _ => $"{value} {unit.ToString().ToLowerInvariant()}"
        };
    }
}

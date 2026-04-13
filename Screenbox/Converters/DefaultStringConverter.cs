using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Converts a <see langword="null"/> or an empty value to a display default string,
/// otherwise returns the original value unchanged.
/// </summary>
/// <remarks>
/// Use the <see cref="DefaultStringConverter"/> when you want to show a stable
/// readable placeholder instead of blank text.
/// </remarks>
internal sealed class DefaultStringConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the default string to display when the value is <see langword="null"/>
    /// or empty.
    /// </summary>
    /// <value>The fallback display string. The default is an empty string.</value>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    /// Gets the specified value, or a localized default string.
    /// </summary>
    /// <param name="value">The input string to evaluate.</param>
    /// <returns>
    /// The <paramref name="value"/> if it is not <see langword="null"/> or empty;
    /// otherwise, a localized default string.
    /// </returns>
    public static string GetValueOrDefault(string value)
    {
        return string.IsNullOrEmpty(value) ? Strings.Resources.Default : value;
    }

    /// <summary>
    /// Gets the specified value, or the provided fallback default string.
    /// </summary>
    /// <param name="value">The input string to evaluate.</param>
    /// <param name="fallback">The fallback default string.</param>
    /// <returns>
    /// The <paramref name="value"/> if it is not <see langword="null"/> or empty;
    /// otherwise, <paramref name="fallback"/>.
    /// </returns>
    public static string GetValueOrFallback(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            null => Default,
            string str => string.IsNullOrEmpty(str) ? Default : str,
            _ => value
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}


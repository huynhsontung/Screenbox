#nullable enable

namespace Screenbox.Converters;

/// <summary>
/// Provides <see langword="static"/> methods for use in XAML data binding scenarios.
/// </summary>
public sealed class BooleanConverter
{
    /// <summary>Inverts the specified <see langword="bool"/> value.</summary>
    /// <param name="value">The boolean value to invert to.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> is <see langword="false"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Invert(bool value)
    {
        return !value;
    }
}

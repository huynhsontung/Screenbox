#nullable enable

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> extension methods for boolean operations used
/// in XAML data binding and general application code.
/// </summary>
public static class BooleanExtensions
{
    /// <summary>
    /// Returns a new <see langword="bool"/> structure whose value is the negated
    /// value of this instance.
    /// </summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> is <see langword="false"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Negate(this bool value)
    {
        return !value;
    }
}

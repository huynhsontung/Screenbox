#nullable enable

using System.Collections;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> extension methods for collection-related
/// operations used in XAML data binding and general application code.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Determines whether the specified <paramref name="collection"/> is
    /// <see langword="null"/> or empty.
    /// </summary>
    /// <param name="collection">The collection to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="collection"/> is <see langword="null"/>
    /// or contains no items; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsNullOrEmpty(this ICollection? collection)
    {
        return collection is null || collection.Count == 0;
    }
}

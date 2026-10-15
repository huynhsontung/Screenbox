using System;
using System.Collections.Generic;

namespace Screenbox.Core.Helpers;

public static class GenreHelpers
{
    private static readonly char[] GenreDelimiters = [',', ';'];

    /// <summary>
    /// Splits a genre metadata string by common delimiters (',' and ';'),
    /// trimming whitespace and omitting empty entries.
    /// </summary>
    public static IEnumerable<string> SplitGenres(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return [];
        }

        return genre.Split(GenreDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Determines whether the given genre property contains the target genre,
    /// taking into account delimiter splitting and case-insensitive matching.
    /// </summary>
    public static bool MatchesGenre(string? genreProperty, string targetGenre)
    {
        if (string.IsNullOrWhiteSpace(genreProperty))
        {
            return false;
        }

        if (!genreProperty.Contains(',') && !genreProperty.Contains(';'))
        {
            return string.Equals(genreProperty.Trim(), targetGenre, StringComparison.CurrentCultureIgnoreCase);
        }

        foreach (string part in genreProperty.Split(GenreDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(part, targetGenre, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

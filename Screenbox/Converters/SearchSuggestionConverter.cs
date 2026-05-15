using Screenbox.Core.Enums;
using Windows.UI.Xaml;

namespace Screenbox.Converters;

public sealed class SearchSuggestionConverter
{
    /// <summary>
    /// Gets the search suggestion name based on the specified suggestion type.
    /// </summary>
    /// <param name="text">The display text of the suggestion.</param>
    /// <param name="type">The category of the suggestion.</param>
    /// <returns>
    /// The query text if <paramref name="type"/> is not <see cref="SearchSuggestionType.None"/>;
    /// otherwise, a localized "no results" message containing the query string.
    /// </returns>
    public static string GetName(SearchSuggestionType type, string text)
    {
        return type is not SearchSuggestionType.None
            ? text
            : Strings.Resources.SearchNoResults(text);
    }

    /// <summary>
    /// Gets the screen reader search suggestion name based on the specified
    /// suggestion type.
    /// </summary>
    /// <param name="text">The query text of the current search.</param>
    /// <param name="type">The category of the suggestion.</param>
    /// <returns>
    /// The query text prefixed with the appropriate category label or a "no results"
    /// message if <paramref name="type"/> is <see cref="SearchSuggestionType.None"/>.
    /// </returns>
    public static string GetAutomationName(SearchSuggestionType type, string text)
    {
        return type switch
        {
            SearchSuggestionType.None => Strings.Resources.SearchNoResults(text),
            SearchSuggestionType.Song => $"{Strings.Resources.Song} {text}",
            SearchSuggestionType.Album => $"{Strings.Resources.PropertyAlbum} {text}",
            SearchSuggestionType.Artist => $"{Strings.Resources.Artist} {text}",
            SearchSuggestionType.Video => $"{Strings.Resources.Video} {text}",
            _ => text,
        };
    }

    /// <summary>
    /// Gets the search suggestion icon visibility based on the specified suggestion kind.
    /// </summary>
    /// <param name="type">The suggestion category of the current search.</param>
    /// <returns>
    /// <see cref="Visibility.Collapsed"/> if <paramref name="type"/> is <see cref="SearchSuggestionType.None"/>;
    /// otherwise, <see cref="Visibility.Visible"/>.</returns>
    public static Visibility GetGlyphVisibility(SearchSuggestionType type)
    {
        return type is SearchSuggestionType.None ? Visibility.Collapsed : Visibility.Visible;
    }
}

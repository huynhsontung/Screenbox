using Screenbox.Core.Enums;
using Windows.UI.Xaml;

namespace Screenbox.Converters;

public sealed class SearchSuggestionConverter
{
    /// <summary>
    /// Gets the search suggestion name based on the specified suggestion kind.
    /// </summary>
    /// <param name="name">The query text of the current search.</param>
    /// <param name="type">The suggestion category of the current search.</param>
    /// <returns>
    /// The query text if <paramref name="type"/> is not <see cref="SearchSuggestionKind.None"/>;
    /// otherwise, a localized "no results" message containing the query string.
    /// </returns>
    public static string GetName(string name, SearchSuggestionKind? type)
    {
        return type == SearchSuggestionKind.None ? Strings.Resources.SearchNoResults(name) : name;
    }

    /// <summary>
    /// Gets the screen reader search suggestion name based on the specified suggestion kind.
    /// </summary>
    /// <param name="name">The query text of the current search.</param>
    /// <param name="type">The suggestion category of the current search.</param>
    /// <returns>
    /// The query text prefixed with the appropriate category label or
    /// a "no results" message if <paramref name="type"/> is <see cref="SearchSuggestionKind.None"/>.
    /// </returns>
    public static string GetAutomationName(string name, SearchSuggestionKind? type)
    {
        return type switch
        {
            SearchSuggestionKind.None => Strings.Resources.SearchNoResults(name),
            SearchSuggestionKind.Song => $"{Strings.Resources.Song} {name}",
            SearchSuggestionKind.Album => $"{Strings.Resources.PropertyAlbum} {name}",
            SearchSuggestionKind.Artist => $"{Strings.Resources.Artist} {name}",
            SearchSuggestionKind.Video => $"{Strings.Resources.Video} {name}",
            _ => name
        };
    }

    /// <summary>
    /// Gets the search suggestion icon visibility based on the specified suggestion kind.
    /// </summary>
    /// <param name="type">The suggestion category of the current search.</param>
    /// <returns>
    /// <see cref="Visibility.Collapsed"/> if <paramref name="type"/> is <see cref="SearchSuggestionKind.None"/>;
    /// otherwise, <see cref="Visibility.Visible"/>.</returns>
    public static Visibility GetGlyphVisibility(SearchSuggestionKind? type)
    {
        return type == SearchSuggestionKind.None ? Visibility.Collapsed : Visibility.Visible;
    }
}

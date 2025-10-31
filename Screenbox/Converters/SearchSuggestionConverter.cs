using Screenbox.Core.Enums;
using Windows.UI.Xaml;

namespace Screenbox.Converters;

public sealed class SearchSuggestionConverter
{
    public static string GetName(string name, SearchSuggestionKind? suggestionKind)
    {
        return suggestionKind == SearchSuggestionKind.None ? Strings.Resources.SearchNoResults(name) : name;
    }

    public static string GetAutomationName(SearchSuggestionKind? value, string name)
    {
        return value switch
        {
            SearchSuggestionKind.Song => $"{Strings.Resources.Song} {name}",
            SearchSuggestionKind.Album => $"{Strings.Resources.PropertyAlbum} {name}",
            SearchSuggestionKind.Artist => $"{Strings.Resources.Artist} {name}",
            SearchSuggestionKind.Video => $"{Strings.Resources.Video} {name}",
            _ => name
        };
    }

    public static Visibility GetIconVisibility(SearchSuggestionKind? value)
    {
        return value == SearchSuggestionKind.None ? Visibility.Collapsed : Visibility.Visible;
    }
}

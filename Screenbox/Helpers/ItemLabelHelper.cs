#nullable enable

using System;
using Screenbox.Core.Enums;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods for formatting accessible names
/// and captions for UI items.
/// </summary>
public static class ItemLabelHelper
{
    public static string GetAccessibleNameForItem(string title, string subtitle)
    {
        return $"{title}; {subtitle}";
    }

    public static string GetAccessibleNameForItem(string title, double count)
    {
        string caption = Strings.Resources.ItemsCount(count);
        return $"{title}; {caption}";
    }

    public static string GetCaptionForStorageItem(bool isFile, string fileInfo, uint itemCount)
    {
        return isFile ? fileInfo : Strings.Resources.ItemsCount(itemCount);
    }

    public static string GetAccessibleNameForStorageItem(bool isFile, string name, string fileInfo, uint itemsCount)
    {
        string type = isFile ? Strings.Resources.File : Strings.Resources.Folder;
        string caption = isFile ? fileInfo : Strings.Resources.ItemsCount(itemsCount);
        return string.Concat(type, ", ", name, "; ", caption);
    }

    /// <summary>
    /// Gets the accessible name for a search suggestion based on its type.
    /// </summary>
    /// <param name="type">A value indicating the type of the search suggestion.</param>
    /// <param name="text">The text of the search suggestion.</param>
    /// <returns>A string representing the accessible name for the search suggestion.</returns>
    public static string GetAccessibleNameForSearchSuggestion(SearchSuggestionType type, string text)
    {
        string? prefix = type switch
        {
            SearchSuggestionType.Song => Strings.Resources.Song,
            SearchSuggestionType.Album => Strings.Resources.PropertyAlbum,
            SearchSuggestionType.Artist => Strings.Resources.Artist,
            SearchSuggestionType.Video => Strings.Resources.Video,
            _ => null
        };

        return prefix is null ? text : string.Concat(prefix, " ", text);
    }
}

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

    /// <summary>
    /// Gets the specified value, or a localized default string.
    /// </summary>
    /// <param name="value">The input string to evaluate.</param>
    /// <returns>
    /// The specified value if it is not <see langword="null"/> or empty; otherwise, the localized default string.
    /// </returns>
    public static string GetValueOrDefault(string value)
    {
        return string.IsNullOrEmpty(value) ? Strings.Resources.Default : value;
    }

    /// <summary>
    /// Gets the specified value, or a provided fallback string.
    /// </summary>
    /// <param name="value">The input string to evaluate.</param>
    /// <param name="fallback">The fallback string to use when <paramref name="value"/> is empty.</param>
    /// <returns>
    /// The specified value if it is not <see langword="null"/> or empty; otherwise, <paramref name="fallback"/>.
    /// </returns>
    public static string GetValueOrFallback(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    /// <summary>
    /// Gets the localized label for a play/pause command.
    /// </summary>
    /// <param name="isPlaying"><see langword="true"/> if the media is playing; otherwise, <see langword="false"/>.</param>
    /// <returns>
    /// The localized pause label if <paramref name="isPlaying"/> is <see langword="true"/>;
    /// otherwise, the localized play label.
    /// </returns>
    public static string GetPlayPauseLabel(bool isPlaying)
    {
        return isPlaying ? Strings.Resources.Pause : Strings.Resources.Play;
    }
}

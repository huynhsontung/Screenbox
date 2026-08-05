#nullable enable

using System;
using Screenbox.Core;
using Screenbox.Core.Enums;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods for formatting accessible names
/// and captions for UI items.
/// </summary>
public static class ItemLabelHelper
{
    private const string CommaSeparator = ", ";
    private const string ItemSeparator = "; ";
    private const string SpaceSeparator = " ";

    /// <summary>
    /// Formats an accessible name using a primary title and a secondary subtitle.
    /// </summary>
    /// <param name="title">The primary title of the item.</param>
    /// <param name="subtitle">The secondary subtitle of the item.</param>
    /// <returns>
    /// A formatted accessible name consisting of <paramref name="title"/> and
    /// <paramref name="subtitle"/> separated by <see cref="ItemSeparator"/>.
    /// </returns>
    public static string FormatAccessibleName(string title, string subtitle)
    {
        return string.Concat(title, ItemSeparator, subtitle);
    }

    /// <summary>
    /// Formats an accessible name using a title and a numeric item count.
    /// </summary>
    /// <param name="title">The primary title of the item.</param>
    /// <param name="count">The numeric count associated with the item.</param>
    /// <returns>
    /// A formatted accessible name consisting of <paramref name="title"/> and a localized
    /// item count caption separated by <see cref="ItemSeparator"/>.
    /// </returns>
    public static string FormatAccessibleName(string title, double count)
    {
        string caption = Strings.Resources.ItemsCount(count);
        return string.Concat(title, ItemSeparator, caption);
    }

    /// <summary>
    /// Formats an accessible name using a title and a duration value.
    /// </summary>
    /// <param name="title">The primary title of the item.</param>
    /// <param name="duration">The duration associated with the item.</param>
    /// <returns>
    /// A formatted accessible name consisting of <paramref name="title"/> and a humanized
    /// duration caption separated by <see cref="ItemSeparator"/>.
    /// </returns>
    public static string FormatAccessibleName(string title, TimeSpan duration)
    {
        string caption = Humanizer.ToDuration(duration);
        return string.Concat(title, ItemSeparator, caption);
    }

    /// <summary>
    /// Formats a play/pause label combined with a contextual item title.
    /// </summary>
    /// <param name="isPlaying"><see langword="true"/> if the item is playing; otherwise <see langword="false"/>.</param>
    /// <param name="title">The contextual title associated with the media item.</param>
    /// <returns>
    /// A formatted label consisting of a localized play/pause command and the item title.
    /// </returns>
    public static string FormatPlayPauseLabel(bool isPlaying, string title)
    {
        string playPauseText = isPlaying ? Strings.Resources.Pause : Strings.Resources.Play;
        return string.Concat(playPauseText, SpaceSeparator, title);
    }

    /// <summary>
    /// Formats an accessible name for a storage item based on whether it is a file or folder.
    /// </summary>
    /// <param name="isFile"><see langword="true"/> if the item is a file; otherwise <see langword="false"/>.</param>
    /// <param name="name">The name of the storage item.</param>
    /// <param name="fileInfo">The file information string.</param>
    /// <param name="itemsCount">The number of items contained in the folder.</param>
    /// <returns>A formatted accessible name consisting of the item type, name, and caption.</returns>
    public static string FormatAccessibleStorageItemName(bool isFile, string name, string fileInfo, uint itemsCount)
    {
        string type = isFile ? Strings.Resources.File : Strings.Resources.Folder;
        string caption = isFile ? fileInfo : Strings.Resources.ItemsCount(itemsCount);
        return string.Concat(type, CommaSeparator, name, ItemSeparator, caption);
    }

    /// <summary>
    /// Formats an accessible name for a search suggestion based on its type.
    /// </summary>
    /// <param name="type">A value indicating the type of the search suggestion.</param>
    /// <param name="text">The text of the search suggestion.</param>
    /// <returns>
    /// A formatted accessible name consisting of a localized prefix and the suggestion text,
    /// or <paramref name="text"/> if no prefix applies.
    /// </returns>
    public static string FormatAccessibleSearchSuggestionName(SearchSuggestionType type, string text)
    {
        string? prefix = type switch
        {
            SearchSuggestionType.Song => Strings.Resources.Song,
            SearchSuggestionType.Album => Strings.Resources.PropertyAlbum,
            SearchSuggestionType.Artist => Strings.Resources.Artist,
            SearchSuggestionType.Video => Strings.Resources.Video,
            _ => null
        };

        return prefix is null ? text : string.Concat(prefix, SpaceSeparator, text);
    }

    /// <summary>
    /// Gets the localized label for a media property.
    /// </summary>
    /// <param name="property">The media property to localize.</param>
    /// <returns>The localized media property label.</returns>
    public static string GetMediaPropertyResourceString(MediaProperty property)
    {
        return property switch
        {
            MediaProperty.Title => Strings.Resources.PropertyTitle,
            MediaProperty.Subtitle => Strings.Resources.PropertySubtitle,
            MediaProperty.Year => Strings.Resources.PropertyYear,
            MediaProperty.Producers => Strings.Resources.PropertyProducers,
            MediaProperty.Writers => Strings.Resources.PropertyWriters,
            MediaProperty.Length => Strings.Resources.PropertyLength,
            MediaProperty.Resolution => Strings.Resources.PropertyResolution,
            MediaProperty.BitRate => Strings.Resources.PropertyBitRate,
            MediaProperty.ContributingArtists => Strings.Resources.PropertyContributingArtists,
            MediaProperty.Album => Strings.Resources.PropertyAlbum,
            MediaProperty.AlbumArtist => Strings.Resources.PropertyAlbumArtist,
            MediaProperty.Composers => Strings.Resources.PropertyComposers,
            MediaProperty.Genre => Strings.Resources.PropertyGenre,
            MediaProperty.Track => Strings.Resources.PropertyTrack,
            MediaProperty.FileType => Strings.Resources.PropertyFileType,
            MediaProperty.ContentType => Strings.Resources.PropertyContentType,
            MediaProperty.Size => Strings.Resources.PropertySize,
            MediaProperty.LastModified => Strings.Resources.PropertyLastModified,
            _ => property.ToString()
        };
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

    /// <summary>
    /// Gets a caption describing a storage item based on whether it is a file or folder.
    /// </summary>
    /// <param name="isFile"><see langword="true"/> if the item is a file; otherwise <see langword="false"/>.</param>
    /// <param name="fileInfo">The file information string.</param>
    /// <param name="itemCount">The number of items contained in the folder.</param>
    /// <returns><paramref name="fileInfo"/> if the item is a file; otherwise a localized item count caption.</returns>
    public static string GetStorageItemCaption(bool isFile, string fileInfo, uint itemCount)
    {
        return isFile ? fileInfo : Strings.Resources.ItemsCount(itemCount);
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
}

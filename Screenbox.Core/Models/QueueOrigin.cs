using System;
using System.Security.Cryptography;
using System.Text;
using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// Metadata describing the source container or mode that created a <see cref="QueueSession"/>.
/// </summary>
/// <param name="Kind">The recent item kind associated with this queue origin.</param>
/// <param name="SourceId">The stable identifier for the origin source (e.g. playlist ID, album key, artist key, or path).</param>
/// <param name="Title">The display title for the recent entry or session.</param>
/// <param name="Subtitle">The optional subtitle or caption for the recent entry or session.</param>
/// <param name="Token">The optional WinRT access cache token for restoring file or folder access across sessions.</param>
public sealed record QueueOrigin(
    RecentItemKind Kind,
    string SourceId,
    string Title,
    string? Subtitle = null,
    string? Token = null)
{
    /// <summary>
    /// Gets a value indicating whether each track change in this queue should be recorded
    /// individually as a <see cref="RecentItemKind.SingleMedia"/> entry.
    /// </summary>
    public bool TrackItemsIndividually =>
        Kind == RecentItemKind.SingleMedia ||
        (Kind == RecentItemKind.MediaCollection && string.IsNullOrEmpty(SourceId));

    /// <summary>Creates a <see cref="QueueOrigin"/> for a single media item.</summary>
    public static QueueOrigin ForSingleMedia(MediaViewModel media)
    {
        return new QueueOrigin(RecentItemKind.SingleMedia, media.Location, media.Name, media.Caption);
    }

    /// <summary>Creates a <see cref="QueueOrigin"/> for a library-wide queue where each played track is recorded individually.</summary>
    public static QueueOrigin ForLibraryQueue(MediaViewModel initialMedia) => ForSingleMedia(initialMedia);

    /// <summary>Creates a <see cref="QueueOrigin"/> for a folder or playlist file origin.</summary>
    public static QueueOrigin ForMediaCollection(string title, string? sourcePath, string? token = null)
    {
        return new QueueOrigin(RecentItemKind.MediaCollection, sourcePath ?? string.Empty, title, Token: token);
    }

    /// <summary>Creates a <see cref="QueueOrigin"/> for a durable user playlist.</summary>
    public static QueueOrigin ForPlaylist(PlaylistViewModel playlist)
    {
        return new QueueOrigin(RecentItemKind.Playlist, playlist.Id, playlist.Name);
    }

    /// <summary>Creates a <see cref="QueueOrigin"/> for an album.</summary>
    public static QueueOrigin ForAlbum(AlbumViewModel album)
    {
        return new QueueOrigin(RecentItemKind.Album, album.AlbumKey, album.Name, album.ArtistName);
    }

    /// <summary>Creates a <see cref="QueueOrigin"/> for an artist.</summary>
    public static QueueOrigin ForArtist(ArtistViewModel artist)
    {
        return new QueueOrigin(RecentItemKind.Artist, artist.ArtistKey, artist.Name);
    }

    /// <summary>
    /// Checks whether this origin matches the specified container kind and source identifier.
    /// </summary>
    public bool Matches(RecentItemKind kind, string sourceId)
    {
        return Kind == kind &&
               !TrackItemsIndividually &&
               string.Equals(NormalizeLocation(SourceId), NormalizeLocation(sourceId), StringComparison.Ordinal);
    }

    /// <summary>
    /// Computes a deterministic SHA-256 content hash for comparing recent entries.
    /// </summary>
    public static string ComputeContentHash(RecentItemKind kind, string sourceOrTitleKey)
    {
        using var sha256 = SHA256.Create();
        string payload = $"{(int)kind}|{NormalizeLocation(sourceOrTitleKey)}";
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a media location or source key for comparison (case-insensitive for file paths, ordinal for non-file URIs).
    /// </summary>
    public static string NormalizeLocation(string location)
    {
        if (string.IsNullOrEmpty(location))
        {
            return string.Empty;
        }

        return Uri.TryCreate(location, UriKind.Absolute, out Uri? uri) && !uri.IsFile
            ? location
            : location.ToLowerInvariant();
    }
}

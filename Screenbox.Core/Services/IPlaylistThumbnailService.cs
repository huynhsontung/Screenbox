#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Services;

/// <summary>
/// Manages generation and disk caching of playlist thumbnail collages.
/// </summary>
public interface IPlaylistThumbnailService
{
    /// <summary>
    /// Reads the cached thumbnail bytes for a playlist, or returns <see langword="null"/> if no cache exists.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <param name="cancellationToken">A token to cancel the read operation.</param>
    /// <returns>The encoded thumbnail bytes, or <see langword="null"/> if unavailable.</returns>
    Task<byte[]?> ReadThumbnailAsync(string playlistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a full-bleed thumbnail or collage from the playlist's items, caches it atomically to disk,
    /// and returns the encoded image bytes. Deletes any existing cache and returns <see langword="null"/>
    /// if no artwork is available.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <param name="items">Snapshot of media items in the playlist.</param>
    /// <param name="cancellationToken">A token to cancel generation and caching.</param>
    /// <returns>The encoded thumbnail bytes, or <see langword="null"/> if no artwork could be generated.</returns>
    Task<byte[]?> BuildAndCacheThumbnailAsync(
        string playlistId,
        IReadOnlyList<MediaViewModel> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the cached thumbnail for a playlist if one exists.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteThumbnailAsync(string playlistId);
}

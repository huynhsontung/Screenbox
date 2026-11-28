using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service for user playlist persistence and export/import operations.
/// </summary>
public interface IPlaylistService
{
    /// <summary>
    /// Save a persistent playlist to storage.
    /// </summary>
    Task SavePlaylistAsync(PlaylistRecordDto playlist);

    /// <summary>
    /// Load a persistent playlist from storage.
    /// </summary>
    Task<PlaylistRecordDto?> LoadPlaylistAsync(string id);

    /// <summary>
    /// List persistent playlists from storage.
    /// </summary>
    Task<IReadOnlyList<PlaylistRecordDto>> ListPlaylistsAsync();

    /// <summary>
    /// Delete a persistent playlist from storage.
    /// </summary>
    Task DeletePlaylistAsync(string id);

    /// <summary>
    /// Appends media items to an existing persistent playlist and persists the updated playlist.
    /// </summary>
    Task AddToPlaylistAsync(string playlistId, IReadOnlyList<MediaViewModel> items);

    /// <summary>
    /// Import playlist entries from a playlist file.
    /// </summary>
    Task<IReadOnlyList<MediaViewModel>> ImportPlaylistItemsAsync(StorageFile file);

    /// <summary>
    /// Export playlist entries to a UTF-8 m3u8 file.
    /// </summary>
    Task ExportPlaylistItemsAsync(IReadOnlyList<MediaViewModel> items, StorageFile file);

    /// <summary>
    /// Asynchronously saves a thumbnail image for a playlist, replacing any existing thumbnail.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <param name="inputStream">The stream that contains the thumbnail image data to copy.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveThumbnailAsync(string playlistId, IRandomAccessStream inputStream);

    /// <summary>
    /// Asynchronously deletes the cached thumbnail for the playlist.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteThumbnailAsync(string playlistId);

    /// <summary>
    /// Asynchronously gets the cached thumbnail file for a playlist.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <returns>
    /// A task that represents the asynchronous get operation. The value of the
    /// task contains the thumbnail of the playlist, or <see langword="null"/>
    /// if no thumbnail exists for the playlist.
    /// </returns>
    Task<StorageFile?> GetThumbnailFileAsync(string playlistId);
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Storage;

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
}

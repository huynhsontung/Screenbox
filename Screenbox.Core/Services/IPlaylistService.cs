#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service for playlist operations
/// </summary>
public interface IPlaylistService
{
    /// <summary>
    /// Add neighboring files to an existing playlist
    /// </summary>
    Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuffle the items in a playlist
    /// </summary>
    Playlist ShufflePlaylist(Playlist playlist, int? preserveIndex = null);

    /// <summary>
    /// Restore playlist from shuffle backup
    /// </summary>
    Playlist RestoreFromShuffle(Playlist playlist);

    /// <summary>
    /// Get media buffer indices around current position
    /// </summary>
    IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5);

    /// <summary>
    /// Save a persistent playlist to storage
    /// </summary>
    Task SavePlaylistAsync(PersistentPlaylist playlist);

    /// <summary>
    /// Load a persistent playlist from storage
    /// </summary>
    Task<PersistentPlaylist?> LoadPlaylistAsync(string id);

    /// <summary>
    /// List persistent playlists from storage
    /// </summary>
    Task<IReadOnlyList<PersistentPlaylist>> ListPlaylistsAsync();

    /// <summary>
    /// Delete a persistent playlist from storage
    /// </summary>
    Task DeletePlaylistAsync(string id);

    /// <summary>
    /// Save a thumbnail for a media item
    /// </summary>
    Task SaveThumbnailAsync(string mediaLocation, byte[] imageBytes);

    /// <summary>
    /// Get a thumbnail file for a media item
    /// </summary>
    Task<StorageFile?> GetThumbnailFileAsync(string mediaLocation);
}

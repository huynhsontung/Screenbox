using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
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
    /// Creates a playlist from storage items, optionally associating a neighboring files query.
    /// </summary>
    Task<Playlist?> TryCreatePlaylistAsync(IReadOnlyList<IStorageItem> storageItems, StorageFileQueryResult? neighboringFilesQuery = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds neighboring files to an existing playlist using the playlist's attached neighboring files query.
    /// </summary>
    Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuffle the items in a playlist
    /// </summary>
    Playlist ShufflePlaylist(Playlist playlist, int? preserveIndex = null);

    /// <summary>
    /// Restore playlist from shuffle backup
    /// </summary>
    Playlist RestoreFromShuffle(Playlist playlist);

    /// <summary>
    /// Synchronizes shuffle backup state when items are added, removed, or replaced in the playlist.
    /// </summary>
    void UpdateShuffleBackup(Playlist playlist, NotifyCollectionChangedEventArgs e);

    /// <summary>
    /// Get media buffer indices around current position
    /// </summary>
    IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5);

    /// <summary>
    /// Save a persistent playlist to storage
    /// </summary>
    Task SavePlaylistAsync(PlaylistRecordDto playlist);

    /// <summary>
    /// Load a persistent playlist from storage
    /// </summary>
    Task<PlaylistRecordDto?> LoadPlaylistAsync(string id);

    /// <summary>
    /// List persistent playlists from storage
    /// </summary>
    Task<IReadOnlyList<PlaylistRecordDto>> ListPlaylistsAsync();

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

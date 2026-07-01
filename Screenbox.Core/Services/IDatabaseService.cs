#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

/// <summary>
/// Manages the application's SQLite database.
/// The database acts as a quick cache layer; data loss is handled gracefully by recreating the database.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Initializes the database, creating it if necessary and applying the schema.
    /// If corruption is detected, the database file is deleted and recreated from scratch.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Reads cached library folders and media records for the requested media type.
    /// </summary>
    Task<RawCacheLoadResultDto> LoadLibraryCacheAsync(MediaPlaybackType mediaType);

    /// <summary>Saves the complete cached music snapshot to the database.</summary>
    Task SaveMusicCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<MusicCacheRecordDto> records);

    /// <summary>Saves the complete cached video snapshot to the database.</summary>
    Task SaveVideoCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<VideoCacheRecordDto> records);

    /// <summary>Replaces playback progress rows with the provided snapshot.</summary>
    Task ReplacePlaybackProgressAsync(IReadOnlyList<MediaPlaybackProgress> snapshot);

    /// <summary>Loads all playback progress entries.</summary>
    Task<List<MediaPlaybackProgress>> LoadPlaybackProgressAsync();

    /// <summary>Persists a playlist and its items.</summary>
    Task SavePlaylistAsync(PersistentPlaylist playlist);

    /// <summary>Loads a playlist and its items, or null when not found.</summary>
    Task<PersistentPlaylist?> LoadPlaylistAsync(string id);

    /// <summary>Lists all playlists with their items.</summary>
    Task<List<PersistentPlaylist>> ListPlaylistsAsync();

    /// <summary>Deletes a playlist and cascades to its items.</summary>
    Task DeletePlaylistAsync(string id);
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Data;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Services;

/// <summary>
/// Repository interface for the Screenbox SQLite cache database.
/// The database acts as a quick cache layer; data loss is handled gracefully
/// by triggering a full disk recrawl.
/// </summary>
public interface IScreenboxDatabase
{
    /// <summary>
    /// Initialises the database, creating or recreating the schema as needed.
    /// Also deletes any legacy <c>.bin</c> cache files from previous app versions.
    /// </summary>
    Task InitializeAsync();

    // ── Library cache ────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces all cached records and folder paths for the given <paramref name="libraryType"/>.
    /// </summary>
    Task SaveLibraryCacheAsync(
        MediaPlaybackType libraryType,
        IEnumerable<MediaRecordEntity> records,
        IEnumerable<string> folderPaths);

    /// <summary>
    /// Returns all cached records and folder paths for the given <paramref name="libraryType"/>.
    /// </summary>
    Task<(List<MediaRecordEntity> Records, List<string> FolderPaths)> LoadLibraryCacheAsync(
        MediaPlaybackType libraryType);

    /// <summary>
    /// Removes all cached records and folder paths for the given <paramref name="libraryType"/>.
    /// </summary>
    Task ClearLibraryCacheAsync(MediaPlaybackType libraryType);

    // ── Playback progress ────────────────────────────────────────────────────

    /// <summary>
    /// Returns all stored playback-progress entries ordered by <see cref="PlaybackProgressEntity.SortOrder"/>.
    /// </summary>
    Task<List<PlaybackProgressEntity>> GetAllPlaybackProgressesAsync();

    /// <summary>
    /// Atomically replaces all playback-progress entries with the supplied <paramref name="items"/>.
    /// </summary>
    Task SaveAllPlaybackProgressesAsync(IEnumerable<PlaybackProgressEntity> items);

    /// <summary>
    /// Removes the playback-progress entry for the given <paramref name="location"/>, if any.
    /// </summary>
    Task RemovePlaybackProgressAsync(string location);

    /// <summary>
    /// Deletes all playback-progress entries.
    /// </summary>
    Task ClearPlaybackProgressesAsync();
}

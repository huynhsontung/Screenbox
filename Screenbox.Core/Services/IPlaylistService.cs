#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Windows.Media;
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
    /// List persistent playlists from storage
    /// </summary>
    Task<IReadOnlyList<PersistentPlaylist>> ListPlaylistsAsync();
}

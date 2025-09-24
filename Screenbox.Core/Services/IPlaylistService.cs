#nullable enable

using Screenbox.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services
{
    /// <summary>
    /// Stateless service for playlist operations
    /// </summary>
    public interface IPlaylistService
    {
        /// <summary>
        /// Create a new playlist from storage items
        /// </summary>
        Task<Playlist> CreatePlaylistAsync(IReadOnlyList<IStorageItem> storageItems, StorageFile? playNext = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add neighboring files to an existing playlist
        /// </summary>
        Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery, StorageFile currentFile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Shuffle the items in a playlist
        /// </summary>
        Playlist ShufflePlaylist(Playlist playlist, int? preserveIndex = null);

        /// <summary>
        /// Restore playlist from shuffle backup
        /// </summary>
        Playlist RestoreFromShuffle(Playlist playlist, ShuffleBackup shuffleBackup);

        /// <summary>
        /// Get next item index based on current state
        /// </summary>
        int? GetNextIndex(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, StorageFileQueryResult? neighboringFilesQuery = null);

        /// <summary>
        /// Get previous item index based on current state
        /// </summary>
        int? GetPreviousIndex(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode);

        /// <summary>
        /// Get media buffer indices around current position
        /// </summary>
        IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5);
    }
}

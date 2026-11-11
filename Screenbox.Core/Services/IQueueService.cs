using System;
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
/// Stateless service for play queue session and media queue operations.
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Creates a queue session from storage items, optionally associating a neighboring files query.
    /// </summary>
    Task<QueueSession?> TryCreateQueueAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses storage items into a queue session, optionally designating a target file to play first and expanding playlist files.
    /// </summary>
    Task<QueueSession?> TryParseQueueAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFile? playNext = null,
        bool expandPlaylists = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a queue session from a single media item, expanding it if it is an expandable media source.
    /// </summary>
    Task<QueueSession> ParseQueueAsync(MediaViewModel media, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a queue session from a storage file, expanding it if it is a playlist file.
    /// </summary>
    Task<QueueSession> ParseQueueAsync(StorageFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a queue session from a URI, expanding it if it is an expandable media source.
    /// </summary>
    Task<QueueSession> ParseQueueAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds neighboring files to an existing queue session using the session's attached neighboring files query.
    /// </summary>
    Task<QueueSession> AddNeighboringFilesAsync(QueueSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuffles the items in a queue session.
    /// </summary>
    QueueSession Shuffle(QueueSession session, int? preserveIndex = null);

    /// <summary>
    /// Restores a queue session from its shuffle backup.
    /// </summary>
    QueueSession RestoreFromShuffle(QueueSession session);

    /// <summary>
    /// Synchronizes shuffle backup state when items are added, removed, or replaced in the queue session.
    /// </summary>
    void UpdateShuffleBackup(QueueSession session, NotifyCollectionChangedEventArgs e);

    /// <summary>
    /// Gets media buffer indices around the current playback position.
    /// </summary>
    IReadOnlyList<int> GetMediaBufferIndices(
        int currentIndex,
        int queueCount,
        MediaPlaybackAutoRepeatMode repeatMode,
        int bufferSize = 5);
}

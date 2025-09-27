#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Factories;

public interface IPlaylistFactory
{
    /// <summary>
    /// Create a playlist from storage items
    /// </summary>
    Task<Playlist> CreatePlaylistAsync(IReadOnlyList<IStorageItem> storageItems, StorageFile? playNext = null, Playlist? reference = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a single media item
    /// </summary>
    Task<Playlist> CreatePlaylistAsync(MediaViewModel media, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a storage file
    /// </summary>
    Task<Playlist> CreatePlaylistAsync(StorageFile file, Playlist? reference = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a URI
    /// </summary>
    Task<Playlist> CreatePlaylistAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse sub-media items recursively (for playlist files)
    /// </summary>
    Task<List<MediaViewModel>> ParseSubMediaRecursiveAsync(MediaViewModel source, CancellationToken cancellationToken = default);
}

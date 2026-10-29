using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Factories;

public interface IMediaListFactory
{
    /// <summary>
    /// Create a playlist from storage items
    /// </summary>
    Task<Playlist?> TryParseMediaListAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFile? playNext = null,
        bool expandPlaylists = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a single media item
    /// </summary>
    Task<Playlist> ParseMediaListAsync(MediaViewModel media, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a storage file
    /// </summary>
    Task<Playlist> ParseMediaListAsync(StorageFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a URI
    /// </summary>
    Task<Playlist> ParseMediaListAsync(Uri uri, CancellationToken cancellationToken = default);
}

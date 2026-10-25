using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Factories;

public interface IMediaListFactory
{
    /// <summary>
    /// Create a playlist from storage items
    /// </summary>
    Task<NextMediaList?> TryParseMediaListAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFile? playNext = null,
        bool expandPlaylists = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a single media item
    /// </summary>
    Task<NextMediaList> ParseMediaListAsync(MediaViewModel media, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a storage file
    /// </summary>
    Task<NextMediaList> ParseMediaListAsync(StorageFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a playlist from a URI
    /// </summary>
    Task<NextMediaList> ParseMediaListAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expands an expandable media source into child <see cref="MediaViewModel"/> items.
    /// </summary>
    Task<IReadOnlyList<MediaViewModel>> ExpandMediaAsync(
        IExpandableMediaSource expandable,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for sub-items on a playback item and converts them into <see cref="MediaViewModel"/> items.
    /// </summary>
    Task<IReadOnlyList<MediaViewModel>> ExpandSubItemsAsync(
        IPlaybackItem playbackItem,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

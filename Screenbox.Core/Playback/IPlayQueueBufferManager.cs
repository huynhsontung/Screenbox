using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;

namespace Screenbox.Core.Playback;

/// <summary>
/// Manages the sliding-window buffer of media items around the current play position
/// and the lifecycle of native <see cref="IPlaybackItem"/> instances.
/// </summary>
public interface IPlayQueueBufferManager : IDisposable
{
    /// <summary>
    /// Gets the queue items currently in the active buffer window.
    /// </summary>
    IReadOnlyList<QueueItemViewModel> BufferedItems { get; }

    /// <summary>
    /// Attempts to retrieve an already buffered playback item for the specified queue item without creating a new one.
    /// </summary>
    bool TryGetPlaybackItem(QueueItemViewModel queueItem, [NotNullWhen(true)] out IPlaybackItem? item);

    /// <summary>
    /// Attempts to retrieve an already buffered playback item for any queued slot matching the specified media without creating a new one.
    /// </summary>
    bool TryGetPlaybackItem(MediaViewModel vm, [NotNullWhen(true)] out IPlaybackItem? item);

    /// <summary>
    /// Gets an existing playback item or creates a new one for the specified queue item.
    /// </summary>
    IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, QueueItemViewModel queueItem);

    /// <summary>
    /// Evicts and disposes the cached playback item for the specified queue item.
    /// </summary>
    void CleanPlaybackItem(QueueItemViewModel queueItem);

    /// <summary>
    /// Evicts and disposes all cached playback items associated with the specified media, optionally excluding one queue item.
    /// </summary>
    void CleanPlaybackItems(MediaViewModel vm, QueueItemViewModel? exclude = null);

    /// <summary>
    /// Clears and disposes all buffered playback items and resets the buffer window.
    /// </summary>
    void Clear();

    /// <summary>
    /// Updates the sliding buffer window around the current index: pre-creates playback items
    /// and evicts items that slid out of the window.
    /// </summary>
    void UpdateBuffer(IMediaPlayer? player, QueueSession session, MediaPlaybackAutoRepeatMode repeatMode);
}

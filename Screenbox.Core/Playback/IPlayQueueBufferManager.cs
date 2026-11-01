using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Screenbox.Core.MediaSources;
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
    /// Gets the media items currently in the active buffer window.
    /// </summary>
    IReadOnlyList<MediaViewModel> BufferedItems { get; }

    /// <summary>
    /// Attempts to retrieve an already buffered playback item matching the specified media source without creating a new one.
    /// </summary>
    bool TryGetPlaybackItem(IMediaSource source, [NotNullWhen(true)] out IPlaybackItem? item);

    /// <summary>
    /// Gets an existing playback item or creates a new one for the specified media.
    /// </summary>
    IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, MediaViewModel vm);

    /// <summary>
    /// Evicts and disposes the cached playback item for the specified media.
    /// </summary>
    void CleanPlaybackItem(MediaViewModel vm);

    /// <summary>
    /// Clears and disposes all buffered playback items and resets the buffer window.
    /// </summary>
    void Clear();

    /// <summary>
    /// Updates the sliding buffer window around the current index: pre-creates playback items
    /// and evicts items that slid out of the window.
    /// </summary>
    void UpdateBuffer(IMediaPlayer? player, Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode);
}

using System;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;

namespace Screenbox.Core.Playback;

/// <summary>
/// Manages the sliding-window buffer of media items around the current play position,
/// pre-fetching thumbnails and managing the lifecycle of native <see cref="IPlaybackItem"/> instances.
/// </summary>
public interface IPlayQueueBufferManager : IDisposable
{
    /// <summary>
    /// Gets an existing playback item or creates a new one for the specified media.
    /// </summary>
    IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, MediaViewModel vm);

    /// <summary>
    /// Evicts and disposes the cached playback item for the specified media.
    /// </summary>
    void CleanPlaybackItem(MediaViewModel vm);

    /// <summary>
    /// Clears and disposes all buffered playback items and resets the thumbnail buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Updates the sliding buffer window around the current index: pre-creates playback items,
    /// evicts items that slid out of the window, and triggers thumbnail loads.
    /// </summary>
    Task UpdateBufferAsync(IMediaPlayer? player, Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode);
}

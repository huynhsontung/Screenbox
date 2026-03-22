#nullable enable

using System.Threading.Tasks;
using Screenbox.Core.Models;
using Windows.Media;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

public interface IPlaybackControlService
{
    /// <summary>
    /// Check if next track is available
    /// </summary>
    bool CanNext(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None, bool hasNeighbor = false);

    /// <summary>
    /// Check if previous track is available
    /// </summary>
    bool CanPrevious(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None, bool hasNeighbor = false);

    /// <summary>
    /// Get the next media file in the folder using a files query
    /// </summary>
    Task<PlaybackNavigationResult?> GetNeighboringNextAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery);

    /// <summary>
    /// Get the previous media file in the folder using a files query
    /// </summary>
    Task<PlaybackNavigationResult?> GetNeighboringPreviousAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery);

    /// <summary>
    /// Get the next media item to play
    /// </summary>
    PlaybackNavigationResult? GetNext(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None);

    /// <summary>
    /// Get the previous media item to play
    /// </summary>
    PlaybackNavigationResult? GetPrevious(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None);

    /// <summary>
    /// Handle end of media playback
    /// </summary>
    PlaybackNavigationResult? HandleMediaEnded(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode);
}

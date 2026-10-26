using System.Threading.Tasks;
using Screenbox.Core.Models;
using Windows.Media;

namespace Screenbox.Core.Services;

public interface IPlaybackControlService
{
    /// <summary>
    /// Check if next track is available
    /// </summary>
    bool CanNext(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None);

    /// <summary>
    /// Check if previous track is available
    /// </summary>
    bool CanPrevious(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None);

    /// <summary>
    /// Asynchronously gets the next media item or neighboring file to play.
    /// </summary>
    Task<PlaybackNavigationResult?> GetNextAsync(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None);

    /// <summary>
    /// Asynchronously gets the previous media item or neighboring file to play.
    /// </summary>
    Task<PlaybackNavigationResult?> GetPreviousAsync(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None);

    /// <summary>
    /// Handle end of media playback
    /// </summary>
    PlaybackNavigationResult? HandleMediaEnded(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode);
}

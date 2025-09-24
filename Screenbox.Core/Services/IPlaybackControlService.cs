#nullable enable

using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;

namespace Screenbox.Core.Services
{
    public interface IPlaybackControlService
    {
        /// <summary>
        /// Check if next track is available
        /// </summary>
        bool CanNext(Playlist playlist);

        /// <summary>
        /// Check if previous track is available
        /// </summary>
        bool CanPrevious(Playlist playlist);

        /// <summary>
        /// Get the next media item to play
        /// </summary>
        Task<PlaybackNavigationResult> GetNextAsync(Playlist playlist);

        /// <summary>
        /// Get the previous media item to play
        /// </summary>
        Task<PlaybackNavigationResult> GetPreviousAsync(Playlist playlist);

        /// <summary>
        /// Handle end of media playback
        /// </summary>
        Task<PlaybackNavigationResult> HandleMediaEndedAsync(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode);
    }

    /// <summary>
    /// Result of a playback navigation operation
    /// </summary>
    public sealed class PlaybackNavigationResult
    {
        public MediaViewModel? NextItem { get; }
        public Playlist? UpdatedPlaylist { get; }
        public bool RequiresNeighboringFileNavigation { get; }

        public PlaybackNavigationResult(MediaViewModel? nextItem)
        {
            NextItem = nextItem;
        }

        public PlaybackNavigationResult(Playlist updatedPlaylist, MediaViewModel? nextItem)
        {
            UpdatedPlaylist = updatedPlaylist;
            NextItem = nextItem;
        }

        public static PlaybackNavigationResult NeighboringFileRequired() =>
            new(null, true);

        private PlaybackNavigationResult(MediaViewModel? nextItem, bool requiresNeighboringFileNavigation)
        {
            NextItem = nextItem;
            RequiresNeighboringFileNavigation = requiresNeighboringFileNavigation;
        }
    }
}

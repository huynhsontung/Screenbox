#nullable enable

using System.Threading.Tasks;
using Screenbox.Core.Models;
using Windows.Media;
using Windows.Storage;

namespace Screenbox.Core.Services
{
    public sealed class PlaybackControlService : IPlaybackControlService
    {
        private readonly IFilesService _filesService;
        private readonly IPlaylistService _playlistService;
        private readonly IMediaParsingService _mediaParsingService;

        public PlaybackControlService(
            IFilesService filesService,
            IPlaylistService playlistService,
            IMediaParsingService mediaParsingService)
        {
            _filesService = filesService;
            _playlistService = playlistService;
            _mediaParsingService = mediaParsingService;
        }

        public bool CanNext(Playlist playlist)
        {
            if (playlist.Items.Count == 1)
            {
                return playlist.NeighboringFilesQuery != null;
            }

            return playlist.CurrentIndex >= 0 && playlist.CurrentIndex < playlist.Items.Count - 1;
        }

        public bool CanPrevious(Playlist playlist)
        {
            if (playlist.Items.Count == 1)
            {
                return playlist.NeighboringFilesQuery != null;
            }

            return playlist.CurrentIndex > 0 && playlist.CurrentIndex - 1 < playlist.Items.Count;
        }

        public async Task<PlaybackNavigationResult> GetNextAsync(Playlist playlist)
        {
            if (playlist.Items.Count == 0 || playlist.CurrentItem == null)
                return new PlaybackNavigationResult(null);

            // Single file with neighboring files
            if (playlist.Items.Count == 1 && playlist.NeighboringFilesQuery != null && playlist.CurrentItem.Source is StorageFile file)
            {
                var nextFile = await _filesService.GetNextFileAsync(file, playlist.NeighboringFilesQuery);
                if (nextFile != null)
                {
                    var result = await _mediaParsingService.CreatePlaylistAsync(nextFile);
                    var newPlaylist = new Playlist(result.Items)
                    {
                        CurrentIndex = result.Items.IndexOf(result.PlayNext),
                        NeighboringFilesQuery = playlist.NeighboringFilesQuery,
                        ShuffleMode = playlist.ShuffleMode,
                        ShuffleBackup = playlist.ShuffleBackup,
                        LastUpdated = playlist.LastUpdated
                    };
                    return new PlaybackNavigationResult(newPlaylist, result.PlayNext);
                }
                return new PlaybackNavigationResult(null);
            }

            // Normal next navigation
            if (playlist.CurrentIndex >= 0 && playlist.CurrentIndex < playlist.Items.Count - 1)
            {
                return new PlaybackNavigationResult(playlist.Items[playlist.CurrentIndex + 1]);
            }

            // At the end - no repeat mode means stop
            return new PlaybackNavigationResult(null);
        }

        public async Task<PlaybackNavigationResult> GetPreviousAsync(Playlist playlist)
        {
            if (playlist.Items.Count == 0 || playlist.CurrentItem == null)
                return new PlaybackNavigationResult(null);

            // Single file with neighboring files
            if (playlist.Items.Count == 1 && playlist.NeighboringFilesQuery != null && playlist.CurrentItem.Source is StorageFile file)
            {
                var previousFile = await _filesService.GetPreviousFileAsync(file, playlist.NeighboringFilesQuery);
                if (previousFile != null)
                {
                    var result = await _mediaParsingService.CreatePlaylistAsync(previousFile);
                    var newPlaylist = new Playlist(result.Items)
                    {
                        CurrentIndex = result.Items.IndexOf(result.PlayNext),
                        NeighboringFilesQuery = playlist.NeighboringFilesQuery,
                        ShuffleMode = playlist.ShuffleMode,
                        ShuffleBackup = playlist.ShuffleBackup,
                        LastUpdated = playlist.LastUpdated
                    };
                    return new PlaybackNavigationResult(newPlaylist, result.PlayNext);
                }
                return new PlaybackNavigationResult(null);
            }

            // Normal previous navigation
            if (playlist.CurrentIndex >= 1 && playlist.CurrentIndex < playlist.Items.Count)
            {
                return new PlaybackNavigationResult(playlist.Items[playlist.CurrentIndex - 1]);
            }

            // At the beginning - no repeat mode means stop
            return new PlaybackNavigationResult(null);
        }

        public async Task<PlaybackNavigationResult> HandleMediaEndedAsync(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode)
        {
            switch (repeatMode)
            {
                case MediaPlaybackAutoRepeatMode.List when playlist.CurrentIndex == playlist.Items.Count - 1:
                    return new PlaybackNavigationResult(playlist.Items[0]);

                case MediaPlaybackAutoRepeatMode.Track:
                    // Track repeat is handled by the media player itself
                    return new PlaybackNavigationResult(null);

                default:
                    if (playlist.Items.Count > 1)
                    {
                        return await GetNextAsync(playlist);
                    }
                    break;
            }

            return new PlaybackNavigationResult(null);
        }
    }
}

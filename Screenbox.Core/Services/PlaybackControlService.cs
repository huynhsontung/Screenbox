#nullable enable

using System.Threading.Tasks;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Windows.Media;
using Windows.Storage;

namespace Screenbox.Core.Services;

public sealed class PlaybackControlService : IPlaybackControlService
{
    private readonly IFilesService _filesService;
    private readonly IMediaListFactory _mediaListFactory;

    public PlaybackControlService(
        IFilesService filesService,
        IMediaListFactory mediaListFactory)
    {
        _filesService = filesService;
        _mediaListFactory = mediaListFactory;
    }

    public bool CanNext(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        // With list repeat, we can always go next if there are items
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && playlist.Items.Count > 0)
        {
            return true;
        }

        // Single file with neighboring files
        if (playlist.Items.Count == 1)
        {
            return playlist.NeighboringFilesQuery != null;
        }

        return playlist.CurrentIndex >= 0 && playlist.CurrentIndex < playlist.Items.Count - 1;
    }

    public bool CanPrevious(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        // We can always go back even when there is only one item in the queue
        // If there is no previous item then the player will just restart the current item
        return playlist.Items.Count > 0 && playlist.CurrentIndex >= 0;
    }

    public async Task<PlaybackNavigationResult?> GetNextAsync(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (playlist.Items.Count == 0 || playlist.CurrentItem == null)
            return null;

        // Single file with neighboring files
        if (playlist.Items.Count == 1 && playlist.NeighboringFilesQuery != null && playlist.CurrentItem.Source is StorageFile file)
        {
            var nextFile = await _filesService.GetNextFileAsync(file, playlist.NeighboringFilesQuery);
            if (nextFile != null)
            {
                var result = await _mediaListFactory.ParseMediaListAsync(nextFile);
                var newPlaylist = new Playlist(result.NextItem, result.Items, playlist);
                return new PlaybackNavigationResult(newPlaylist, result.NextItem);
            }
            return null;
        }

        return GetNext(playlist, repeatMode);
    }

    private PlaybackNavigationResult? GetNext(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (playlist.Items.Count == 0 || playlist.CurrentItem == null)
            return null;

        // Normal next navigation
        if (playlist.CurrentIndex >= 0 && playlist.CurrentIndex < playlist.Items.Count - 1)
        {
            return new PlaybackNavigationResult(playlist.Items[playlist.CurrentIndex + 1]);
        }

        // At the end - handle repeat mode
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && playlist.Items.Count > 0)
        {
            // Loop back to first item
            return new PlaybackNavigationResult(playlist.Items[0]);
        }

        // No repeat mode means stop
        return null;
    }

    public async Task<PlaybackNavigationResult?> GetPreviousAsync(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (playlist.Items.Count == 0 || playlist.CurrentItem == null)
            return null;

        // Single file with neighboring files
        if (playlist.Items.Count == 1 && playlist.NeighboringFilesQuery != null && playlist.CurrentItem.Source is StorageFile file)
        {
            var previousFile = await _filesService.GetPreviousFileAsync(file, playlist.NeighboringFilesQuery);
            if (previousFile != null)
            {
                var result = await _mediaListFactory.ParseMediaListAsync(previousFile);
                var newPlaylist = new Playlist(result.NextItem, result.Items, playlist);
                return new PlaybackNavigationResult(newPlaylist, result.NextItem);
            }
            return null;
        }

        return GetPrevious(playlist, repeatMode);
    }

    private PlaybackNavigationResult? GetPrevious(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (playlist.Items.Count == 0 || playlist.CurrentItem == null)
            return null;

        // Normal previous navigation
        if (playlist.CurrentIndex >= 1 && playlist.CurrentIndex < playlist.Items.Count)
        {
            return new PlaybackNavigationResult(playlist.Items[playlist.CurrentIndex - 1]);
        }

        // At the beginning - handle repeat mode
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && playlist.Items.Count > 0)
        {
            // Loop back to last item
            return new PlaybackNavigationResult(playlist.Items[playlist.Items.Count - 1]);
        }

        // No repeat mode means stop
        return null;
    }

    public PlaybackNavigationResult? HandleMediaEnded(Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode)
    {
        switch (repeatMode)
        {
            case MediaPlaybackAutoRepeatMode.List when playlist.CurrentIndex == playlist.Items.Count - 1:
                return new PlaybackNavigationResult(playlist.Items[0]);

            case MediaPlaybackAutoRepeatMode.Track:
                // Track repeat is handled by the media player itself
                return null;

            default:
                if (playlist.Items.Count > 1)
                {
                    return GetNext(playlist, repeatMode);
                }
                break;
        }

        return null;
    }
}

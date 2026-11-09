using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Windows.Media;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

public sealed class PlaybackControlService : IPlaybackControlService
{
    private readonly IFilesService _filesService;
    private readonly IQueueService _queueService;

    public PlaybackControlService(
        IFilesService filesService,
        IQueueService queueService)
    {
        _filesService = filesService;
        _queueService = queueService;
    }

    public bool CanNext(QueueSession session, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        // With list repeat, we can always go next if there are items
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && session.Items.Count > 0) return true;

        // With a single item, we can only go next if there is a neighboring file
        if (session.Items.Count == 1 && session.NeighboringFilesQuery is not null) return true;

        return session.CurrentIndex >= 0 && session.CurrentIndex < session.Items.Count - 1;
    }

    public bool CanPrevious(QueueSession session, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        // We can always go back even when there is only one item in the queue
        // If there is no previous item then the player will just restart the current item
        return session.Items.Count > 0 && session.CurrentIndex >= 0;
    }

    public async Task<PlaybackNavigationResult?> GetNextAsync(QueueSession session, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (session.Items.Count == 1 && session.NeighboringFilesQuery is { } query)
        {
            return await GetNeighboringNextAsync(session, query);
        }

        return GetNext(session, repeatMode);
    }

    public async Task<PlaybackNavigationResult?> GetPreviousAsync(QueueSession session, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (session.Items.Count == 1 && session.NeighboringFilesQuery is { } query)
        {
            return await GetNeighboringPreviousAsync(session, query);
        }

        return GetPrevious(session, repeatMode);
    }

    public PlaybackNavigationResult? HandleMediaEnded(QueueSession session, MediaPlaybackAutoRepeatMode repeatMode)
    {
        switch (repeatMode)
        {
            case MediaPlaybackAutoRepeatMode.List when session.CurrentIndex == session.Items.Count - 1:
                return new PlaybackNavigationResult(session.Items[0]);

            case MediaPlaybackAutoRepeatMode.Track:
                // Track repeat is handled by the media player itself
                return null;

            default:
                if (session.Items.Count > 1)
                {
                    return GetNext(session, repeatMode);
                }
                break;
        }

        return null;
    }

    private async Task<PlaybackNavigationResult?> GetNeighboringNextAsync(QueueSession session, StorageFileQueryResult neighboringFilesQuery)
    {
        // Single file with neighboring files
        if (session.Items.Count == 1 && session.CurrentItem?.Source is IFileMediaSource fileSource && await fileSource.GetStorageFileAsync() is { } file)
        {
            var nextFile = await _filesService.GetNextFileAsync(file, neighboringFilesQuery);
            if (nextFile is not null)
            {
                var result = await _queueService.ParseQueueAsync(nextFile);
                var newQueue = new QueueSession(result.CurrentIndex, result.Items, session);
                if (newQueue.CurrentItem is { } nextItem)
                {
                    return new PlaybackNavigationResult(newQueue, nextItem);
                }
            }
        }

        return null;
    }

    private PlaybackNavigationResult? GetNext(QueueSession session, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (session.Items.Count == 0 || session.CurrentItem is null)
            return null;

        // Normal next navigation
        if (session.CurrentIndex >= 0 && session.CurrentIndex < session.Items.Count - 1)
        {
            return new PlaybackNavigationResult(session.Items[session.CurrentIndex + 1]);
        }

        // At the end - handle repeat mode
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && session.Items.Count > 0)
        {
            // Loop back to first item
            return new PlaybackNavigationResult(session.Items[0]);
        }

        // No repeat mode means stop
        return null;
    }

    private async Task<PlaybackNavigationResult?> GetNeighboringPreviousAsync(QueueSession session, StorageFileQueryResult neighboringFilesQuery)
    {
        // Single file with neighboring files
        if (session.Items.Count == 1 && session.CurrentItem?.Source is IFileMediaSource fileSource && await fileSource.GetStorageFileAsync() is { } file)
        {
            var previousFile = await _filesService.GetPreviousFileAsync(file, neighboringFilesQuery);
            if (previousFile is not null)
            {
                var result = await _queueService.ParseQueueAsync(previousFile);
                var newQueue = new QueueSession(result.CurrentIndex, result.Items, session);
                if (newQueue.CurrentItem is { } nextItem)
                {
                    return new PlaybackNavigationResult(newQueue, nextItem);
                }
            }
        }

        return null;
    }

    private PlaybackNavigationResult? GetPrevious(QueueSession session, MediaPlaybackAutoRepeatMode repeatMode = MediaPlaybackAutoRepeatMode.None)
    {
        if (session.Items.Count == 0 || session.CurrentItem is null)
            return null;

        // Normal previous navigation
        if (session.CurrentIndex >= 1 && session.CurrentIndex < session.Items.Count)
        {
            return new PlaybackNavigationResult(session.Items[session.CurrentIndex - 1]);
        }

        // At the beginning - handle repeat mode
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && session.Items.Count > 0)
        {
            // Loop back to last item
            return new PlaybackNavigationResult(session.Items[session.Items.Count - 1]);
        }

        // No repeat mode means stop
        return null;
    }
}

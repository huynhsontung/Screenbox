using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// Result of a playback navigation operation
/// </summary>
public sealed class PlaybackNavigationResult
{
    public MediaViewModel NextItem { get; }
    public QueueSession? UpdatedQueue { get; }

    public PlaybackNavigationResult(MediaViewModel nextItem)
    {
        NextItem = nextItem;
    }

    public PlaybackNavigationResult(QueueSession updatedQueue, MediaViewModel nextItem) : this(nextItem)
    {
        UpdatedQueue = updatedQueue;
    }
}

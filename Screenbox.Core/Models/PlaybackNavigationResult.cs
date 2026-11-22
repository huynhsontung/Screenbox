using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// Result of a playback navigation operation
/// </summary>
public sealed class PlaybackNavigationResult
{
    public QueueItemViewModel NextItem { get; }
    public QueueSession? UpdatedQueue { get; }

    public PlaybackNavigationResult(QueueItemViewModel nextItem)
    {
        NextItem = nextItem;
    }

    public PlaybackNavigationResult(QueueSession updatedQueue, QueueItemViewModel nextItem) : this(nextItem)
    {
        UpdatedQueue = updatedQueue;
    }
}

#nullable enable

using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

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

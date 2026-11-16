using System.Threading.Tasks;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Coordinates the lifecycle, library linking, and persistence of Recent Media entries.
/// </summary>
public interface IRecentMediaCoordinator
{
    /// <summary>
    /// Loads recent media entries from persistent storage into <see cref="Contexts.RecentContext"/>.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Loads details and thumbnails for items in the recent media list.
    /// </summary>
    Task LoadDetailsAndThumbnailsAsync();

    /// <summary>
    /// Removes a single recent entry from the list and persistent storage.
    /// </summary>
    Task RemoveAsync(IRecentItem item);

    /// <summary>
    /// Clears all recent entries from the list and persistent storage.
    /// </summary>
    Task ClearAsync();
}

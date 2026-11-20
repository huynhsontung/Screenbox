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
    /// <param name="forceReload">
    /// <see langword="true"/> to bypass the cached loaded state and reload entries from storage.
    /// </param>
    Task LoadAsync(bool forceReload = false);

    /// <summary>
    /// Loads metadata details for single media items in the recent media list.
    /// </summary>
    Task LoadDetailsAsync();

    /// <summary>
    /// Removes a single recent entry from the list and persistent storage.
    /// </summary>
    Task RemoveAsync(IRecentItem item);

    /// <summary>
    /// Clears all recent entries from the list and persistent storage.
    /// </summary>
    Task ClearAsync();
}

#nullable enable

using System;
using System.Threading.Tasks;

namespace Screenbox.Core.Services;

/// <summary>
/// Tracks and persists the last playback position for each media item,
/// enabling resume-from-position functionality.
/// </summary>
public interface ILastPositionTracker
{
    /// <summary>
    /// Gets a value indicating whether position data has been loaded from disk.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the timestamp of the most recent position update or clear operation.
    /// </summary>
    DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// Records or updates the last playback position for the given media location.
    /// </summary>
    void UpdateLastPosition(string location, TimeSpan position);

    /// <summary>
    /// Returns the last recorded position for the given media location, or <see cref="TimeSpan.Zero"/> if none.
    /// </summary>
    TimeSpan GetPosition(string location);

    /// <summary>
    /// Removes the recorded position for the given media location.
    /// </summary>
    void RemovePosition(string location);

    /// <summary>
    /// Clears all recorded positions.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Persists the current position list to disk.
    /// </summary>
    Task SaveToDiskAsync();

    /// <summary>
    /// Loads the persisted position list from disk.
    /// </summary>
    Task LoadFromDiskAsync();
}

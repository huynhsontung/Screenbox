#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Stateful coordinator that owns the global play queue for the duration of the app session.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PlayQueueCoordinator"/> handles all queue mutations, playback navigation,
/// message routing, Windows System Media Transport Controls wiring, media event handling,
/// neighboring-file auto-enqueue, and thumbnail pre-buffering.
/// </para>
/// <para>
/// All observable state is written to <see cref="Contexts.PlayQueueContext"/>,
/// which ViewModels observe for data binding.
/// </para>
/// </remarks>
public interface IPlayQueueCoordinator
{
    /// <summary>
    /// Raised when the result of <see cref="CanNext"/> or <see cref="CanPrevious"/> changes.
    /// ViewModels should subscribe to keep their own navigation command states in sync.
    /// </summary>
    event EventHandler? CanNavigateChanged;

    /// <summary>
    /// Returns whether navigating to the next item is currently possible.
    /// </summary>
    bool CanNext();

    /// <summary>
    /// Returns whether navigating to the previous item is currently possible.
    /// </summary>
    bool CanPrevious();

    /// <summary>
    /// Advances to the next item in the play queue.
    /// Handles neighboring-file navigation when the queue contains a single item.
    /// </summary>
    Task NextAsync();

    /// <summary>
    /// Returns to the previous item in the play queue.
    /// If the current position is more than 5 seconds in, restarts the current item instead.
    /// </summary>
    Task PreviousAsync();

    /// <summary>Clears the play queue and stops playback.</summary>
    void Clear();

    /// <summary>
    /// Appends or inserts the given storage items into the play queue.
    /// Playlist files (e.g. .m3u) are parsed and their contents added.
    /// </summary>
    /// <param name="files">The files or folders to enqueue.</param>
    /// <param name="insertIndex">
    /// The zero-based index at which to insert. Pass -1 (default) to append at the end.
    /// </param>
    Task EnqueueAsync(IReadOnlyList<IStorageItem> files, int insertIndex = -1);

    /// <summary>
    /// Removes an item from the play queue.
    /// If the item is currently playing, playback is stopped before removal.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    void Remove(MediaViewModel item);

    /// <summary>
    /// Inserts a copy of <paramref name="item"/> immediately after the currently playing item.
    /// </summary>
    /// <param name="item">The item to insert next.</param>
    void InsertNext(MediaViewModel item);

    /// <summary>
    /// Reloads the current item from scratch without changing the queue position.
    /// Useful for recovering from playback failures.
    /// </summary>
    void ResetCurrentItem();
}

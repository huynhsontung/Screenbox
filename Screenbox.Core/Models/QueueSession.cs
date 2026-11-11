using System;
using System.Collections.Generic;
using Screenbox.Core.ViewModels;
using Windows.Storage.Search;

namespace Screenbox.Core.Models;

/// <summary>
/// Pure data model representing an active play queue session.
/// </summary>
public sealed class QueueSession
{
    public List<MediaViewModel> Items { get; }
    public int CurrentIndex { get; set; }
    public bool ShuffleMode { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this queue session.
    /// </summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the origin metadata describing how this queue session was created.
    /// </summary>
    public QueueOrigin? Origin { get; set; }

    /// <summary>
    /// Gets a value indicating whether this queue session has a shuffle backup that can be restored.
    /// </summary>
    public bool CanRestoreFromShuffle => ShuffleBackup is not null;
    internal ShuffleBackup? ShuffleBackup { get; set; }

    /// <summary>
    /// Gets or sets the folder file query for single-file playback navigation.
    /// </summary>
    public StorageFileQueryResult? NeighboringFilesQuery { get; set; }

    public QueueSession() : this(new List<MediaViewModel>()) { }

    public QueueSession(MediaViewModel currentItem, QueueSession? reference = null)
        : this(0, (MediaViewModel[])[currentItem], reference)
    {
    }

    public QueueSession(IReadOnlyList<MediaViewModel> items, QueueSession? reference = null)
    {
        Items = new List<MediaViewModel>(items);
        CurrentIndex = -1;
        ShuffleMode = reference?.ShuffleMode ?? false;
        ShuffleBackup = reference?.ShuffleBackup;
        NeighboringFilesQuery = reference?.NeighboringFilesQuery;
        SessionId = reference?.SessionId ?? Guid.NewGuid();
        Origin = reference?.Origin;
    }

    public QueueSession(int currentIndex, IReadOnlyList<MediaViewModel> items, QueueSession? reference = null)
        : this(items, reference)
    {
        CurrentIndex = currentIndex;
    }

    public QueueSession(MediaViewModel currentItem, IReadOnlyList<MediaViewModel> items, QueueSession? reference = null)
        : this(items, reference)
    {
        CurrentIndex = Items.IndexOf(currentItem);
    }

    public QueueSession(QueueSession reference) : this(reference.Items, reference)
    {
        CurrentIndex = reference.CurrentIndex;
    }

    public bool IsEmpty => Items.Count == 0;

    public MediaViewModel? CurrentItem =>
        CurrentIndex >= 0 && CurrentIndex < Items.Count ? Items[CurrentIndex] : null;

    /// <summary>
    /// Returns the canonical (unshuffled) list of items in this queue session.
    /// When a shuffle backup is present, returns the original queue order with any removed items excluded.
    /// </summary>
    public IReadOnlyList<MediaViewModel> GetCanonicalItems()
    {
        if (ShuffleBackup is null)
        {
            return Items;
        }

        var canonical = new List<MediaViewModel>(ShuffleBackup.OriginalItems);
        foreach (MediaViewModel removal in ShuffleBackup.Removals)
        {
            canonical.Remove(removal);
        }

        return canonical;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.ViewModels;
using Windows.Storage.Search;

namespace Screenbox.Core.Models;

/// <summary>
/// Pure data model representing an active play queue session.
/// </summary>
public sealed class QueueSession
{
    public List<QueueItemViewModel> Items { get; }
    public int CurrentIndex { get; set; }
    public bool ShuffleMode { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this queue session.
    /// </summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets a value indicating whether this queue session has a shuffle backup that can be restored.
    /// </summary>
    public bool CanRestoreFromShuffle => ShuffleBackup is not null;
    internal ShuffleBackup? ShuffleBackup { get; set; }

    /// <summary>
    /// Gets or sets the folder file query for single-file playback navigation.
    /// </summary>
    public StorageFileQueryResult? NeighboringFilesQuery { get; set; }

    public QueueSession() : this(new List<QueueItemViewModel>()) { }

    public QueueSession(QueueItemViewModel currentItem, QueueSession? reference = null)
        : this(0, (QueueItemViewModel[])[currentItem], reference)
    {
    }

    public QueueSession(MediaViewModel currentItem, QueueSession? reference = null, QueueOrigin? origin = null)
        : this(new QueueItemViewModel(currentItem, origin), reference)
    {
    }

    public QueueSession(IReadOnlyList<QueueItemViewModel> items, QueueSession? reference = null)
    {
        Items = new List<QueueItemViewModel>(items);
        CurrentIndex = -1;
        ShuffleMode = reference?.ShuffleMode ?? false;
        ShuffleBackup = reference?.ShuffleBackup;
        NeighboringFilesQuery = reference?.NeighboringFilesQuery;
        SessionId = reference?.SessionId ?? Guid.NewGuid();
    }

    public QueueSession(IReadOnlyList<MediaViewModel> items, QueueSession? reference = null, QueueOrigin? origin = null)
        : this(WrapMediaItems(items, origin), reference)
    {
    }

    public QueueSession(int currentIndex, IReadOnlyList<QueueItemViewModel> items, QueueSession? reference = null)
        : this(items, reference)
    {
        CurrentIndex = currentIndex;
    }

    public QueueSession(int currentIndex, IReadOnlyList<MediaViewModel> items, QueueSession? reference = null, QueueOrigin? origin = null)
        : this(currentIndex, WrapMediaItems(items, origin), reference)
    {
    }

    public QueueSession(QueueItemViewModel currentItem, IReadOnlyList<QueueItemViewModel> items, QueueSession? reference = null)
        : this(items, reference)
    {
        CurrentIndex = Items.IndexOf(currentItem);
    }

    public QueueSession(MediaViewModel currentItem, IReadOnlyList<MediaViewModel> items, QueueSession? reference = null, QueueOrigin? origin = null)
        : this(WrapMediaItems(items, origin), reference)
    {
        CurrentIndex = Items.FindIndex(item => ReferenceEquals(item.Media, currentItem));
    }

    public QueueSession(QueueSession reference) : this(reference.Items, reference)
    {
        CurrentIndex = reference.CurrentIndex;
    }

    public bool IsEmpty => Items.Count == 0;

    public QueueItemViewModel? CurrentItem =>
        CurrentIndex >= 0 && CurrentIndex < Items.Count ? Items[CurrentIndex] : null;

    /// <summary>
    /// Stamps the specified container origin onto every item in this session.
    /// Individual origins (or <see langword="null"/>) clear item container origins.
    /// </summary>
    public void SetOrigin(QueueOrigin? origin)
    {
        QueueOrigin? containerOrigin = origin is { TrackItemsIndividually: false } ? origin : null;
        foreach (QueueItemViewModel item in Items)
        {
            item.Origin = containerOrigin;
        }
    }

    private static List<QueueItemViewModel> WrapMediaItems(IReadOnlyList<MediaViewModel> items, QueueOrigin? origin)
    {
        QueueOrigin? containerOrigin = origin is { TrackItemsIndividually: false } ? origin : null;
        return items.Select(item => new QueueItemViewModel(item, containerOrigin)).ToList();
    }
}

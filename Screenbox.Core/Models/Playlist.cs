using System;
using System.Collections.Generic;
using Screenbox.Core.ViewModels;
using Windows.Storage.Search;

namespace Screenbox.Core.Models;

/// <summary>
/// Pure data model representing a playlist
/// </summary>
public sealed class Playlist
{
    public List<MediaViewModel> Items { get; }
    public int CurrentIndex { get; set; }
    public bool ShuffleMode { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the queue session this playlist belongs to.
    /// </summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the origin metadata describing how this playlist was created.
    /// </summary>
    public PlaylistOrigin? Origin { get; set; }

    /// <summary>
    /// Gets a value indicating whether this playlist has a shuffle backup that can be restored.
    /// </summary>
    public bool CanRestoreFromShuffle => ShuffleBackup is not null;
    internal ShuffleBackup? ShuffleBackup { get; set; }

    /// <summary>
    /// Gets or sets the folder file query for single-file playback navigation.
    /// </summary>
    public StorageFileQueryResult? NeighboringFilesQuery { get; set; }

    public Playlist() : this(new List<MediaViewModel>()) { }

    public Playlist(MediaViewModel currentItem, Playlist? reference = null)
        : this(0, (MediaViewModel[])[currentItem], reference)
    {
    }

    public Playlist(IReadOnlyList<MediaViewModel> items, Playlist? reference = null)
    {
        Items = new List<MediaViewModel>(items);
        CurrentIndex = -1;
        ShuffleMode = reference?.ShuffleMode ?? false;
        ShuffleBackup = reference?.ShuffleBackup;
        NeighboringFilesQuery = reference?.NeighboringFilesQuery;
        SessionId = reference?.SessionId ?? Guid.NewGuid();
        Origin = reference?.Origin;
    }

    public Playlist(int currentIndex, IReadOnlyList<MediaViewModel> items, Playlist? reference = null)
        : this(items, reference)
    {
        CurrentIndex = currentIndex;
    }

    public Playlist(MediaViewModel currentItem, IReadOnlyList<MediaViewModel> items, Playlist? reference = null)
        : this(items, reference)
    {
        CurrentIndex = Items.IndexOf(currentItem);
    }

    public Playlist(Playlist reference) : this(reference.Items, reference)
    {
        CurrentIndex = reference.CurrentIndex;
    }

    public bool IsEmpty => Items.Count == 0;

    public MediaViewModel? CurrentItem =>
        CurrentIndex >= 0 && CurrentIndex < Items.Count ? Items[CurrentIndex] : null;

    /// <summary>
    /// Returns the canonical (unshuffled) list of items in this playlist.
    /// When a shuffle backup is present, returns the original playlist order with any removed items excluded.
    /// </summary>
    public IReadOnlyList<MediaViewModel> GetCanonicalItems()
    {
        if (ShuffleBackup is null)
        {
            return Items;
        }

        var canonical = new List<MediaViewModel>(ShuffleBackup.OriginalPlaylist);
        foreach (MediaViewModel removal in ShuffleBackup.Removals)
        {
            canonical.Remove(removal);
        }

        return canonical;
    }
}

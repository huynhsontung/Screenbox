#nullable enable

using System.Collections.Generic;
using System.Linq;
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
    internal ShuffleBackup? ShuffleBackup { get; set; }
    public StorageFileQueryResult? NeighboringFilesQuery { get; set; }
    public object? LastUpdated { get; set; }

    public Playlist(Playlist? reference = null) : this(new List<MediaViewModel>(), reference) { }

    public Playlist(IReadOnlyList<MediaViewModel> items, Playlist? reference = null)
    {
        Items = items.ToList();
        CurrentIndex = -1;
        NeighboringFilesQuery = reference?.NeighboringFilesQuery;
        ShuffleMode = reference?.ShuffleMode ?? false;
        ShuffleBackup = reference?.ShuffleBackup;
        LastUpdated = reference?.LastUpdated;
    }

    public Playlist(MediaViewModel currentItem, IReadOnlyList<MediaViewModel> items, Playlist? reference = null)
        : this(items, reference)
    {
        CurrentIndex = Items.IndexOf(currentItem);
    }

    public bool IsEmpty => Items.Count == 0;

    public MediaViewModel? CurrentItem =>
        CurrentIndex >= 0 && CurrentIndex < Items.Count ? Items[CurrentIndex] : null;

    public Playlist Clone()
    {
        return new Playlist(new List<MediaViewModel>(Items))
        {
            CurrentIndex = CurrentIndex,
            ShuffleMode = ShuffleMode,
            ShuffleBackup = ShuffleBackup,
            NeighboringFilesQuery = NeighboringFilesQuery,
            LastUpdated = LastUpdated
        };
    }
}

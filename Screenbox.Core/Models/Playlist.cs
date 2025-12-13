#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// Pure data model representing a playlist
/// </summary>
public class Playlist
{
    public List<MediaViewModel> Items { get; }
    public int CurrentIndex { get; set; }
    public bool ShuffleMode { get; set; }
    internal ShuffleBackup? ShuffleBackup { get; set; }

    public Playlist() : this(new List<MediaViewModel>()) { }

    public Playlist(IReadOnlyList<MediaViewModel> items, Playlist? reference = null)
    {
        Items = new List<MediaViewModel>(items);
        CurrentIndex = -1;
        ShuffleMode = reference?.ShuffleMode ?? false;
        ShuffleBackup = reference?.ShuffleBackup;
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
}

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.ViewModels;
using Windows.Storage.Search;

namespace Screenbox.Core.Models
{
    /// <summary>
    /// Pure data model representing a playlist
    /// </summary>
    public sealed class Playlist
    {
        public List<MediaViewModel> Items { get; }
        public int CurrentIndex { get; set; }
        public bool ShuffleMode { get; set; }
        public ShuffleBackup? ShuffleBackup { get; set; }
        public StorageFileQueryResult? NeighboringFilesQuery { get; set; }
        public object? LastUpdated { get; set; }

        public Playlist() : this(new List<MediaViewModel>()) { }

        public Playlist(IReadOnlyList<MediaViewModel> items)
        {
            Items = items.ToList();
            CurrentIndex = -1;
        }

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

    /// <summary>
    /// Backup data for shuffle functionality
    /// </summary>
    public sealed class ShuffleBackup
    {
        public List<MediaViewModel> OriginalPlaylist { get; }
        public List<MediaViewModel> Removals { get; }

        public ShuffleBackup(List<MediaViewModel> originalPlaylist, List<MediaViewModel>? removals = null)
        {
            OriginalPlaylist = originalPlaylist;
            Removals = removals ?? new List<MediaViewModel>();
        }
    }
}

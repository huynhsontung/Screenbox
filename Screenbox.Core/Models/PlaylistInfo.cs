#nullable enable

using System.Collections.Generic;
using Windows.Storage.Search;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Models;
public sealed class PlaylistInfo
{
    public StorageFileQueryResult? NeighboringFilesQuery { get; }

    public IReadOnlyCollection<MediaViewModel> Playlist { get; }

    public MediaViewModel? ActiveItem { get; }

    public int ActiveIndex { get; }

    public object? LastUpdate { get; }

    public PlaylistInfo(IReadOnlyCollection<MediaViewModel> playlist, MediaViewModel? activeItem, int activeIndex,
        object? lastUpdate, StorageFileQueryResult? neighboringFilesQuery)
    {
        Playlist = playlist;
        ActiveItem = activeItem;
        ActiveIndex = activeIndex;
        LastUpdate = lastUpdate;
        NeighboringFilesQuery = neighboringFilesQuery;
    }
}
using System.Collections.Generic;
using Screenbox.ViewModels;

namespace Screenbox.Core
{
    internal sealed record PlaylistInfo(IReadOnlyCollection<MediaViewModel> Playlist, MediaViewModel? ActiveItem, int ActiveIndex, object? LastUpdate)
    {
        public IReadOnlyCollection<MediaViewModel> Playlist { get; } = Playlist;

        public MediaViewModel? ActiveItem { get; } = ActiveItem;

        public int ActiveIndex { get; } = ActiveIndex;

        public object? LastUpdate { get; } = LastUpdate;
    }
}

#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;
public sealed class PlaylistCreateResult
{
    public MediaViewModel PlayNext { get; }
    public List<MediaViewModel> Items { get; }

    public PlaylistCreateResult(MediaViewModel playNext, List<MediaViewModel> items)
    {
        PlayNext = playNext;
        Items = items;
    }

    public PlaylistCreateResult(MediaViewModel playNext) : this(playNext, new List<MediaViewModel> { playNext }) { }
}

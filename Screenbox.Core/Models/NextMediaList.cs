#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;
public sealed class NextMediaList
{
    public MediaViewModel NextItem { get; }
    public List<MediaViewModel> Items { get; }

    public NextMediaList(MediaViewModel nextItem, List<MediaViewModel> items)
    {
        NextItem = nextItem;
        Items = items;
    }

    public NextMediaList(MediaViewModel nextItem) : this(nextItem, new List<MediaViewModel> { nextItem }) { }
}

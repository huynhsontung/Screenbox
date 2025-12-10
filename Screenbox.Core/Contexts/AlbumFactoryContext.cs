#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

public sealed class AlbumFactoryContext
{
    public Dictionary<string, AlbumViewModel> Albums { get; } = new();
    public AlbumViewModel? UnknownAlbum { get; set; }
}

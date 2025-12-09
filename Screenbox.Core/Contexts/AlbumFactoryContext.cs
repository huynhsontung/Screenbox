#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

internal sealed class AlbumFactoryContext
{
    internal Dictionary<string, AlbumViewModel> Albums { get; } = new();
    internal AlbumViewModel? UnknownAlbum { get; set; }
}

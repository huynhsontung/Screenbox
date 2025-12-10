#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

public sealed class ArtistFactoryContext
{
    public Dictionary<string, ArtistViewModel> Artists { get; } = new();
    public ArtistViewModel? UnknownArtist { get; set; }
}

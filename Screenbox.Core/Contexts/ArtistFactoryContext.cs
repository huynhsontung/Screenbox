#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

internal sealed class ArtistFactoryContext
{
    internal Dictionary<string, ArtistViewModel> Artists { get; } = new();
    internal ArtistViewModel? UnknownArtist { get; set; }
}

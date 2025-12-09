#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

internal sealed class ArtistFactoryState
{
    internal Dictionary<string, ArtistViewModel> Artists { get; } = new();
    internal ArtistViewModel? UnknownArtist { get; set; }
}

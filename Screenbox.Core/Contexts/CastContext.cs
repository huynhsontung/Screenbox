#nullable enable

using System.Collections.Generic;
using LibVLCSharp.Shared;
using Screenbox.Core.Models;

namespace Screenbox.Core.Contexts;

public sealed class CastContext
{
    public List<Renderer> Renderers { get; } = new();
    public RendererDiscoverer? Discoverer { get; set; }
}

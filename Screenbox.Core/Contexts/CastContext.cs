#nullable enable

using System.Collections.Generic;
using LibVLCSharp.Shared;

namespace Screenbox.Core.Contexts;

internal sealed class CastContext
{
    internal List<Renderer> Renderers { get; } = new();
    internal RendererDiscoverer? Discoverer { get; set; }
}

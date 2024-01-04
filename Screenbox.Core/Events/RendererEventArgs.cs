using Screenbox.Core.Models;
using System;

namespace Screenbox.Core.Events
{
    public sealed class RendererFoundEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererFoundEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }

    public sealed class RendererLostEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererLostEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }
}

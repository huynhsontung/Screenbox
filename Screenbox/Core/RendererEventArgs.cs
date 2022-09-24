using System;

namespace Screenbox.Core
{
    internal sealed class RendererFoundEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererFoundEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }

    internal sealed class RendererLostEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererLostEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }
}

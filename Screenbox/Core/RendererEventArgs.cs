using System;

namespace Screenbox.Core
{
    internal class RendererFoundEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererFoundEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }

    internal class RendererLostEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererLostEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }
}

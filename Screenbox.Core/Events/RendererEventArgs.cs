using Screenbox.Casting.Abstractions;
using System;

namespace Screenbox.Core.Events
{
    public sealed class RendererFoundEventArgs : EventArgs
    {
        public ICastDevice Device { get; }

        public RendererFoundEventArgs(ICastDevice device)
        {
            Device = device;
        }
    }

    public sealed class RendererLostEventArgs : EventArgs
    {
        public ICastDevice Device { get; }

        public RendererLostEventArgs(ICastDevice device)
        {
            Device = device;
        }
    }
}

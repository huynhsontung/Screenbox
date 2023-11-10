#nullable enable

using Screenbox.Core.Events;
using System;

namespace Screenbox.Core.Services
{
    public interface ICastService
    {
        event EventHandler<RendererFoundEventArgs>? RendererFound;
        event EventHandler<RendererLostEventArgs>? RendererLost;
        bool SetActiveRenderer(Renderer? renderer);
        bool Start();
        void Stop();
    }
}
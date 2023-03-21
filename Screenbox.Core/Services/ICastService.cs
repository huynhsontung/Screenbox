#nullable enable

using System;
using Screenbox.Core;
using Screenbox.Core.Events;

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
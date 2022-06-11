#nullable enable

using System;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface ICastService
    {
        event EventHandler<RendererFoundEventArgs>? RendererFound;
        event EventHandler<RendererLostEventArgs>? RendererLost;
        bool SetActiveRenderer(Renderer? renderer);
        bool Start();
        void Stop();
    }
}
#nullable enable

using System;
using Screenbox.Core.Events;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

public interface ICastService
{
    event EventHandler<RendererFoundEventArgs>? RendererFound;
    event EventHandler<RendererLostEventArgs>? RendererLost;
    bool SetActiveRenderer(IMediaPlayer player, Renderer? renderer);
    bool Start(IMediaPlayer player);
    void Stop();
}

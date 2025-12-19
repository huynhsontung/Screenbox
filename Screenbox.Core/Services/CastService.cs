#nullable enable

using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

public sealed class CastService : ICastService
{
    public RendererWatcher? CreateRendererWatcher(IMediaPlayer player)
    {
        if (player is not VlcMediaPlayer vlcMediaPlayer)
            return null;

        return new RendererWatcher(vlcMediaPlayer);
    }

    public bool SetActiveRenderer(IMediaPlayer player, Renderer? renderer)
    {
        if (player is not VlcMediaPlayer vlcMediaPlayer) return false;
        return vlcMediaPlayer.VlcPlayer.SetRenderer(renderer?.Target);
    }
}

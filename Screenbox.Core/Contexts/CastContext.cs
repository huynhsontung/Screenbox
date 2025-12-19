#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Contexts;

public sealed partial class CastContext : ObservableObject
{
    [ObservableProperty]
    private RendererWatcher? _rendererWatcher;

    public bool SetActiveRenderer(IMediaPlayer player, Renderer? renderer)
    {
        if (player is not VlcMediaPlayer vlcMediaPlayer) return false;
        return vlcMediaPlayer.VlcPlayer.SetRenderer(renderer?.Target);
    }

    public bool StartDiscovering(IMediaPlayer player)
    {
        StopDiscovering();

        // Get LibVLC from the current media player
        if (player is not VlcMediaPlayer vlcMediaPlayer)
            return false;

        RendererWatcher = new RendererWatcher(vlcMediaPlayer.LibVlc);
        return RendererWatcher.Start();
    }

    public void StopDiscovering()
    {
        if (RendererWatcher == null) return;
        RendererWatcher.Stop();
        RendererWatcher.Dispose();
        RendererWatcher = null;
    }
}

#nullable enable

using System;
using System.Collections.Generic;
using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Screenbox.Core.Events;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

public sealed class CastService : ICastService
{
    public event EventHandler<RendererFoundEventArgs>? RendererFound;
    public event EventHandler<RendererLostEventArgs>? RendererLost;

    private readonly List<Renderer> _renderers;
    private RendererDiscoverer? _discoverer;

    public CastService()
    {
        _renderers = new List<Renderer>();
    }

    public bool SetActiveRenderer(IMediaPlayer player, Renderer? renderer)
    {
        if (player is not VlcMediaPlayer vlcMediaPlayer) return false;
        return vlcMediaPlayer.VlcPlayer.SetRenderer(renderer?.Target);
    }

    public bool Start(IMediaPlayer player)
    {
        Stop();

        // Get LibVLC from the current media player
        if (player is not VlcMediaPlayer vlcMediaPlayer)
            return false;

        _discoverer = new RendererDiscoverer(vlcMediaPlayer.LibVlc);
        _discoverer.ItemAdded += DiscovererOnItemAdded;
        _discoverer.ItemDeleted += DiscovererOnItemDeleted;
        return _discoverer.Start();
    }

    public void Stop()
    {
        if (_discoverer == null) return;
        _discoverer.Stop();
        _discoverer.ItemAdded -= DiscovererOnItemAdded;
        _discoverer.ItemDeleted -= DiscovererOnItemDeleted;
        _discoverer.Dispose();
        _discoverer = null;
        foreach (Renderer renderer in _renderers)
        {
            renderer.Dispose();
        }

        _renderers.Clear();
    }

    private void DiscovererOnItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
    {
        Guard.IsNotNull(_discoverer, nameof(_discoverer));
        Renderer renderer = new(e.RendererItem);
        _renderers.Add(renderer);
        RendererFound?.Invoke(this, new RendererFoundEventArgs(renderer));
    }

    private void DiscovererOnItemDeleted(object sender, RendererDiscovererItemDeletedEventArgs e)
    {
        Renderer? item = _renderers.Find(r => r.Target == e.RendererItem);
        if (item != null)
        {
            RendererLost?.Invoke(this, new RendererLostEventArgs(item));
        }
    }
}

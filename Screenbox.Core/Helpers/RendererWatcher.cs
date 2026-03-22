#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using LibVLCSharp.Shared;
using Screenbox.Core.Events;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Helpers;

public sealed class RendererWatcher : IDisposable
{
    public event EventHandler<RendererFoundEventArgs>? RendererFound;
    public event EventHandler<RendererLostEventArgs>? RendererLost;

    public bool IsStarted { get; private set; }

    private readonly VlcMediaPlayer _vlcMediaPlayer;
    private readonly List<Renderer> _renderers;
    private RendererDiscoverer? _discoverer;

    internal RendererWatcher(VlcMediaPlayer vlcMediaPlayer)
    {
        _vlcMediaPlayer = vlcMediaPlayer;
        _renderers = new List<Renderer>();
    }

    public IReadOnlyList<Renderer> GetRenderers()
    {
        return _renderers.AsReadOnly();
    }

    public bool Start()
    {
        if (IsStarted) return true;

        _discoverer = new RendererDiscoverer(_vlcMediaPlayer.LibVlc);
        _discoverer.ItemAdded += OnItemAdded;
        _discoverer.ItemDeleted += OnItemDeleted;
        bool started = _discoverer.Start();
        if (started)
        {
            IsStarted = true;
        }
        return started;
    }

    public void Stop()
    {
        if (!IsStarted || _discoverer == null) return;

        _discoverer.Stop();
        _discoverer.ItemAdded -= OnItemAdded;
        _discoverer.ItemDeleted -= OnItemDeleted;
        _discoverer.Dispose();
        _discoverer = null;
        IsStarted = false;

        foreach (Renderer renderer in _renderers)
        {
            renderer.Dispose();
        }

        _renderers.Clear();
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
    {
        Renderer renderer = new(e.RendererItem);
        _renderers.Add(renderer);
        RendererFound?.Invoke(this, new RendererFoundEventArgs(renderer));
    }

    private void OnItemDeleted(object sender, RendererDiscovererItemDeletedEventArgs e)
    {
        Renderer? item = _renderers.FirstOrDefault(r => r.Target == e.RendererItem);
        if (item != null)
        {
            _renderers.Remove(item);
            RendererLost?.Invoke(this, new RendererLostEventArgs(item));
        }
    }
}

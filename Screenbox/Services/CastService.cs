#nullable enable

using System;
using System.Collections.Generic;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal class CastService : ICastService
    {
        public event EventHandler<RendererFoundEventArgs>? RendererFound;
        public event EventHandler<RendererLostEventArgs>? RendererLost; 

        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly List<Renderer> _renderers;
        private RendererDiscoverer? _discoverer;

        public CastService(IMediaPlayerService mediaPlayerService)
        {
            _mediaPlayerService = mediaPlayerService;
            _renderers = new List<Renderer>();
        }

        public bool SetActiveRenderer(Renderer? renderer)
        {
            return _mediaPlayerService.VlcPlayer?.SetRenderer(renderer?.Target) ?? false;
        }

        public bool Start()
        {
            Stop();
            LibVLC? libVlc = _mediaPlayerService.LibVlc;
            Guard.IsNotNull(libVlc, nameof(libVlc));
            _discoverer = new RendererDiscoverer(libVlc);
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
}

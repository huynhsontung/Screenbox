#nullable enable

using System;
using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Screenbox.Core.Contexts;
using Screenbox.Core.Events;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services
{
    public sealed class CastService : ICastService
    {
        public event EventHandler<RendererFoundEventArgs>? RendererFound;
        public event EventHandler<RendererLostEventArgs>? RendererLost;

        private readonly LibVlcService _libVlcService;
        private readonly CastContext State;

        public CastService(LibVlcService libVlcService, CastContext state)
        {
            _libVlcService = libVlcService;
            State = state;
        }

        public bool SetActiveRenderer(Renderer? renderer)
        {
            return _libVlcService.MediaPlayer?.VlcPlayer.SetRenderer(renderer?.Target) ?? false;
        }

        public bool Start()
        {
            Stop();
            LibVLC? libVlc = _libVlcService.LibVlc;
            Guard.IsNotNull(libVlc, nameof(libVlc));
            State.Discoverer = new RendererDiscoverer(libVlc);
            State.Discoverer.ItemAdded += DiscovererOnItemAdded;
            State.Discoverer.ItemDeleted += DiscovererOnItemDeleted;
            return State.Discoverer.Start();
        }

        public void Stop()
        {
            if (State.Discoverer == null) return;
            State.Discoverer.Stop();
            State.Discoverer.ItemAdded -= DiscovererOnItemAdded;
            State.Discoverer.ItemDeleted -= DiscovererOnItemDeleted;
            State.Discoverer.Dispose();
            State.Discoverer = null;
            foreach (Renderer renderer in State.Renderers)
            {
                renderer.Dispose();
            }

            State.Renderers.Clear();
        }

        private void DiscovererOnItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            Guard.IsNotNull(State.Discoverer, nameof(State.Discoverer));
            Renderer renderer = new(e.RendererItem);
            State.Renderers.Add(renderer);
            RendererFound?.Invoke(this, new RendererFoundEventArgs(renderer));
        }

        private void DiscovererOnItemDeleted(object sender, RendererDiscovererItemDeletedEventArgs e)
        {
            Renderer? item = State.Renderers.Find(r => r.Target == e.RendererItem);
            if (item != null)
            {
                RendererLost?.Invoke(this, new RendererLostEventArgs(item));
            }
        }
    }
}

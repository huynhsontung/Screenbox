#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Sharpcaster;
using Sharpcaster.Models;
using Screenbox.Core.Events;
using Screenbox.Core.Models;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Watches for Chromecast devices on the local network using SharpCaster's mDNS discovery.
/// Discovery is event-driven via <see cref="ChromecastLocator.StartContinuousDiscovery"/>.
/// </summary>
public sealed class RendererWatcher : IDisposable
{
    public event EventHandler<RendererFoundEventArgs>? RendererFound;
    public event EventHandler<RendererLostEventArgs>? RendererLost;

    /// <summary>Gets a value indicating whether discovery is currently running.</summary>
    public bool IsStarted { get; private set; }

    private readonly List<Renderer> _renderers;
    private ChromecastLocator? _locator;

    internal RendererWatcher()
    {
        _renderers = new List<Renderer>();
    }

    /// <summary>Returns a read-only snapshot of all currently discovered renderers.</summary>
    public IReadOnlyList<Renderer> GetRenderers()
    {
        return _renderers.AsReadOnly();
    }

    /// <summary>Starts continuous mDNS discovery. Returns <c>false</c> if already started.</summary>
    public bool Start()
    {
        if (IsStarted) return true;

        _locator = new ChromecastLocator();
        _locator.ChromecastReceiverFound += OnReceiverFound;
        _locator.StartContinuousDiscovery();

        IsStarted = true;
        return true;
    }

    /// <summary>Stops discovery and marks all known renderers as unavailable.</summary>
    public void Stop()
    {
        if (!IsStarted) return;

        if (_locator is not null)
        {
            _locator.ChromecastReceiverFound -= OnReceiverFound;
            _locator.StopContinuousDiscovery();
            _locator.Dispose();
            _locator = null;
        }

        IsStarted = false;

        foreach (Renderer renderer in _renderers)
        {
            renderer.MarkUnavailable();
            RendererLost?.Invoke(this, new RendererLostEventArgs(renderer));
        }

        _renderers.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Raised by <see cref="ChromecastLocator"/> when a new device is found on the network.
    /// </summary>
    private void OnReceiverFound(object? sender, ChromecastReceiverEventArgs e)
    {
        ChromecastReceiver receiver = e.Receiver;
        if (_renderers.Any(r => r.Name == receiver.Name))
        {
            return;
        }

        Renderer renderer = new(receiver);
        _renderers.Add(renderer);
        RendererFound?.Invoke(this, new RendererFoundEventArgs(renderer));
    }
}

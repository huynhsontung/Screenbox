#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Models;
using Screenbox.Core.Events;
using Screenbox.Core.Models;

namespace Screenbox.Core.Helpers;

public sealed class RendererWatcher : IDisposable
{
    public event EventHandler<RendererFoundEventArgs>? RendererFound;
    public event EventHandler<RendererLostEventArgs>? RendererLost;

    public bool IsStarted { get; private set; }

    private readonly ICastDeviceDiscovery _discovery;
    private readonly List<Renderer> _renderers;
    private readonly ConcurrentDictionary<string, Renderer> _indexById;

    internal RendererWatcher(ICastDeviceDiscovery discovery)
    {
        _discovery = discovery;
        _renderers = new List<Renderer>();
        _indexById = new ConcurrentDictionary<string, Renderer>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Renderer> GetRenderers()
    {
        return _renderers.AsReadOnly();
    }

    public bool Start()
    {
        if (IsStarted) return true;

        _discovery.DeviceFound += OnDeviceFound;
        _discovery.DeviceLost += OnDeviceLost;
        TryAwait(_discovery.StartAsync());
        IsStarted = true;

        foreach (CastDevice device in _discovery.GetDevices())
        {
            OnDeviceFound(this, device);
        }

        return true;
    }

    public void Stop()
    {
        if (!IsStarted) return;

        _discovery.DeviceFound -= OnDeviceFound;
        _discovery.DeviceLost -= OnDeviceLost;
        TryAwait(_discovery.StopAsync());
        IsStarted = false;

        foreach (Renderer renderer in _renderers)
        {
            renderer.Dispose();
        }

        _renderers.Clear();
        _indexById.Clear();
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnDeviceFound(object? sender, CastDevice device)
    {
        if (_indexById.ContainsKey(device.Id)) return;

        Renderer renderer = new(device);
        _renderers.Add(renderer);
        _indexById[renderer.Id] = renderer;
        RendererFound?.Invoke(this, new RendererFoundEventArgs(renderer));
    }

    private void OnDeviceLost(object? sender, CastDevice device)
    {
        if (!_indexById.TryRemove(device.Id, out Renderer? renderer)) return;

        _renderers.Remove(renderer);
        RendererLost?.Invoke(this, new RendererLostEventArgs(renderer));
    }

    private static void TryAwait(Task task)
    {
        try
        {
            task.GetAwaiter().GetResult();
        }
        catch
        {
            // Discovery failures are surfaced via empty renderer list for now.
        }
    }
}

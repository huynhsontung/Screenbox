#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Models;

namespace Screenbox.Casting.Discovery;

/// <summary>
/// In-memory discovery source used during migration before mDNS scanner integration.
/// </summary>
public sealed class ManualCastDeviceDiscovery : ICastDeviceDiscovery
{
    private readonly ConcurrentDictionary<string, CastDevice> _devices = new();

    public event EventHandler<CastDevice>? DeviceFound;

    public event EventHandler<CastDevice>? DeviceLost;

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts manual discovery source.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops manual discovery source.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns current discovered devices.
    /// </summary>
    public IReadOnlyCollection<CastDevice> GetDevices()
    {
        return _devices.Values.ToArray();
    }

    /// <summary>
    /// Adds or updates a discovered device.
    /// </summary>
    public void AddOrUpdate(CastDevice device)
    {
        bool added = _devices.TryAdd(device.Id, device);
        if (!added)
        {
            _devices[device.Id] = device;
        }

        DeviceFound?.Invoke(this, device);
    }

    /// <summary>
    /// Removes a discovered device by identifier.
    /// </summary>
    public bool Remove(string id)
    {
        bool removed = _devices.TryRemove(id, out CastDevice? device);
        if (removed && device is not null)
        {
            DeviceLost?.Invoke(this, device);
        }

        return removed;
    }

    /// <summary>
    /// Releases discovery resources.
    /// </summary>
    public void Dispose()
    {
        IsRunning = false;
        _devices.Clear();
    }
}

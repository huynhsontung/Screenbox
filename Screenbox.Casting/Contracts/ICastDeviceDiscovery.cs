#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Casting.Models;

namespace Screenbox.Casting.Contracts;

/// <summary>
/// Discovery contract for cast-capable devices.
/// </summary>
public interface ICastDeviceDiscovery : IDisposable
{
    event EventHandler<CastDevice>? DeviceFound;

    event EventHandler<CastDevice>? DeviceLost;

    bool IsRunning { get; }

    IReadOnlyCollection<CastDevice> GetDevices();

    /// <summary>
    /// Starts device discovery.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops device discovery.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

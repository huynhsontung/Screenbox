#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Models;
using Zeroconf;

namespace Screenbox.Casting.Discovery;

/// <summary>
/// Chromecast device discovery backed by mDNS/DNS-SD service resolution.
/// </summary>
public sealed class ChromecastMdnsDiscovery : ICastDeviceDiscovery
{
    private const string ChromecastServiceType = "_googlecast._tcp.local.";

    private readonly ConcurrentDictionary<string, CastDevice> _devices = new();

    private ZeroconfResolver.ResolverListener? _listener;

    public event EventHandler<CastDevice>? DeviceFound;

    public event EventHandler<CastDevice>? DeviceLost;

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts continuous Chromecast mDNS discovery.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        _listener = ZeroconfResolver.CreateListener(
            ChromecastServiceType,
            queryInterval: 4000,
            pingsUntilRemove: 2,
            scanTime: TimeSpan.FromSeconds(2),
            retries: 2,
            retryDelayMilliseconds: 2000);

        _listener.ServiceFound += OnServiceFound;
        _listener.ServiceLost += OnServiceLost;
        IsRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops Chromecast mDNS discovery and clears subscriptions.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }

        IsRunning = false;

        if (_listener is not null)
        {
            _listener.ServiceFound -= OnServiceFound;
            _listener.ServiceLost -= OnServiceLost;
            _listener.Dispose();
            _listener = null;
        }

        _devices.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns current discovered Chromecast devices.
    /// </summary>
    public IReadOnlyCollection<CastDevice> GetDevices()
    {
        return _devices.Values.ToArray();
    }

    /// <summary>
    /// Releases discovery resources.
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
    }

    /// <summary>
    /// Converts resolved service information into a cast device and publishes updates.
    /// </summary>
    private void OnServiceFound(object? sender, IZeroconfHost host)
    {
        CastDevice? device = TryMapDevice(host);
        if (device is null)
        {
            return;
        }

        bool added = _devices.TryAdd(device.Id, device);
        if (!added)
        {
            _devices[device.Id] = device;
        }

        DeviceFound?.Invoke(this, device);
    }

    /// <summary>
    /// Removes unavailable cast device and publishes loss notification.
    /// </summary>
    private void OnServiceLost(object? sender, IZeroconfHost host)
    {
        CastDevice? device = TryMapDevice(host);
        if (device is null)
        {
            return;
        }

        bool removed = _devices.TryRemove(device.Id, out CastDevice? removedDevice);
        if (removed && removedDevice is not null)
        {
            DeviceLost?.Invoke(this, removedDevice);
        }
    }

    /// <summary>
    /// Maps Zeroconf host data to the cast device model.
    /// </summary>
    private static CastDevice? TryMapDevice(IZeroconfHost host)
    {
        if (!host.Services.TryGetValue(ChromecastServiceType, out IService service))
        {
            return null;
        }

        string hostAddress = host.IPAddress;
        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            hostAddress = host.IPAddresses?.FirstOrDefault() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(hostAddress) || service.Port <= 0)
        {
            return null;
        }

        IReadOnlyDictionary<string, string> properties = FlattenProperties(service.Properties);
        string id = properties.TryGetValue("id", out string? castId) && !string.IsNullOrWhiteSpace(castId)
            ? castId
            : host.Id;
        string name = properties.TryGetValue("fn", out string? friendlyName) && !string.IsNullOrWhiteSpace(friendlyName)
            ? friendlyName
            : host.DisplayName;
        string model = properties.TryGetValue("md", out string? modelName) ? modelName : string.Empty;

        return new CastDevice(
            id,
            string.IsNullOrWhiteSpace(name) ? hostAddress : name,
            hostAddress,
            service.Port,
            CastProtocol.Chromecast,
            canRenderVideo: true,
            canRenderAudio: true,
            model,
            iconUri: null);
    }

    /// <summary>
    /// Flattens TXT record property values into a single dictionary.
    /// </summary>
    private static IReadOnlyDictionary<string, string> FlattenProperties(IReadOnlyList<IReadOnlyDictionary<string, string>>? properties)
    {
        if (properties is null || properties.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        Dictionary<string, string> flattened = new(StringComparer.OrdinalIgnoreCase);
        foreach (IReadOnlyDictionary<string, string> record in properties)
        {
            foreach (KeyValuePair<string, string> pair in record)
            {
                flattened[pair.Key] = pair.Value;
            }
        }

        return flattened;
    }
}

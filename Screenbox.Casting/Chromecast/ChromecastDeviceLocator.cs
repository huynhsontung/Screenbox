#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Events;
using Sharpcaster;
using Sharpcaster.Models;
using Sharpcaster.Models.Media;

namespace Screenbox.Casting.Chromecast;

/// <summary>
/// Discovers Chromecast devices on the local network using SharpCaster's mDNS scanner
/// and creates <see cref="ChromecastSession"/> instances for them.
/// </summary>
/// <remarks>
/// Discovery is event-driven via <see cref="ChromecastLocator.StartContinuousDiscovery"/>.
/// Create one locator per discovery session and dispose it when the cast picker closes.
/// </remarks>
public sealed class ChromecastDeviceLocator : ICastDeviceLocator
{
    /// <summary>Application ID for the Google Default Media Receiver.</summary>
    private const string DefaultMediaReceiverId = "CC1AD845";

    /// <inheritdoc/>
    public event EventHandler<CastDeviceFoundEventArgs>? DeviceFound;

    /// <inheritdoc/>
    public event EventHandler<CastDeviceRemovedEventArgs>? DeviceLost;

    /// <inheritdoc/>
    public bool IsStarted { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<ICastDevice> Devices => _devices.AsReadOnly();

    private readonly List<ChromecastDevice> _devices;
    private ChromecastLocator? _locator;

    /// <summary>Initialises a new instance of <see cref="ChromecastDeviceLocator"/>.</summary>
    public ChromecastDeviceLocator()
    {
        _devices = new List<ChromecastDevice>();
    }

    /// <inheritdoc/>
    public bool Start()
    {
        if (IsStarted) return false;

        _locator = new ChromecastLocator();
        _locator.ChromecastReceiverFound += OnReceiverFound;
        _locator.StartContinuousDiscovery();

        IsStarted = true;
        return true;
    }

    /// <inheritdoc/>
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

        // Raise DeviceLost for all tracked devices and mark them unavailable.
        foreach (ChromecastDevice device in _devices)
        {
            device.MarkUnavailable();
            DeviceLost?.Invoke(this, new CastDeviceRemovedEventArgs(device));
        }

        _devices.Clear();
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="device"/> was not created by this locator.
    /// </exception>
    public async Task<ICastSession?> ConnectAsync(ICastDevice device, Uri streamUrl, TimeSpan startPosition)
    {
        if (device is not ChromecastDevice chromecastDevice)
        {
            throw new ArgumentException("Device must be a ChromecastDevice created by this locator.", nameof(device));
        }

        return await ChromecastSession.CreateAsync(chromecastDevice, streamUrl, startPosition, DefaultMediaReceiverId).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Handles the <see cref="ChromecastLocator.ChromecastReceiverFound"/> event.
    /// Deduplicates by name and raises <see cref="DeviceFound"/>.
    /// </summary>
    private void OnReceiverFound(object? sender, ChromecastReceiverEventArgs e)
    {
        ChromecastReceiver receiver = e.Receiver;

        // Skip duplicates — the locator may report the same device more than once.
        if (_devices.Any(d => d.Name == receiver.Name))
        {
            return;
        }

        ChromecastDevice newDevice = new(receiver);
        _devices.Add(newDevice);
        DeviceFound?.Invoke(this, new CastDeviceFoundEventArgs(newDevice));
    }
}

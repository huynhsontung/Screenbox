#nullable enable

using System;
using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Models;

namespace Screenbox.Casting.Dlna;

/// <summary>
/// Represents a discovered DLNA/UPnP Digital Media Renderer (DMR) device.
/// </summary>
/// <remarks>
/// Holds the AVTransport and RenderingControl service URLs resolved from the device's
/// UPnP description document. These are used by <see cref="DlnaAvTransportClient"/> and
/// <see cref="DlnaRenderingControlClient"/> to send SOAP commands.
/// </remarks>
public sealed class DlnaDevice : ICastDevice
{
    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public CastDeviceType Type => CastDeviceType.Dlna;

    /// <inheritdoc/>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Gets the absolute URL of the UPnP AVTransport service control endpoint.
    /// Used for sending play/pause/stop/seek commands.
    /// </summary>
    public Uri AvTransportControlUrl { get; }

    /// <summary>
    /// Gets the absolute URL of the UPnP RenderingControl service control endpoint.
    /// Used for setting volume and mute state.
    /// </summary>
    public Uri RenderingControlUrl { get; }

    /// <summary>
    /// Gets a unique identifier for this device (derived from the device description URL).
    /// Used for deduplication during discovery.
    /// </summary>
    internal string UniqueId { get; }

    internal DlnaDevice(string name, Uri avTransportControlUrl, Uri renderingControlUrl, string uniqueId)
    {
        Name = name;
        AvTransportControlUrl = avTransportControlUrl;
        RenderingControlUrl = renderingControlUrl;
        UniqueId = uniqueId;
        IsAvailable = true;
    }

    /// <summary>Marks this device as no longer reachable.</summary>
    internal void MarkUnavailable()
    {
        IsAvailable = false;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({nameof(CastDeviceType.Dlna)})";
}

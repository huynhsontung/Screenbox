#nullable enable

using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Models;
using Sharpcaster.Models;

namespace Screenbox.Casting.Chromecast;

/// <summary>
/// Represents a discovered Chromecast device as a protocol-agnostic <see cref="ICastDevice"/>.
/// </summary>
public sealed class ChromecastDevice : ICastDevice
{
    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public CastDeviceType Type => CastDeviceType.Chromecast;

    /// <inheritdoc/>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the underlying SharpCaster receiver used to connect to this device.</summary>
    internal ChromecastReceiver Receiver { get; }

    internal ChromecastDevice(ChromecastReceiver receiver)
    {
        Receiver = receiver;
        Name = receiver.Name;
        IsAvailable = true;
    }

    /// <summary>Marks this device as no longer reachable (e.g., it left the network).</summary>
    internal void MarkUnavailable()
    {
        IsAvailable = false;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({nameof(CastDeviceType.Chromecast)})";
}

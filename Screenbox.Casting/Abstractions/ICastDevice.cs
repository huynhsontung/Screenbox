#nullable enable

using Screenbox.Casting.Models;

namespace Screenbox.Casting.Abstractions;

/// <summary>
/// Represents a discovered media-casting target device on the local network.
/// </summary>
/// <remarks>
/// Implementations hold the protocol-specific connection info needed by
/// <see cref="ICastDeviceLocator.ConnectAsync"/> to establish a session.
/// </remarks>
public interface ICastDevice
{
    /// <summary>Gets the human-readable display name of the device.</summary>
    string Name { get; }

    /// <summary>Gets the casting protocol this device uses.</summary>
    CastDeviceType Type { get; }

    /// <summary>
    /// Gets a value indicating whether this device is still reachable on the network.
    /// Set to <c>false</c> when the device is lost during discovery.
    /// </summary>
    bool IsAvailable { get; }
}

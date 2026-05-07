#nullable enable

using Screenbox.Casting.Abstractions;
using System;

namespace Screenbox.Casting.Events;

/// <summary>
/// Provides data for the <see cref="ICastDeviceLocator.DeviceFound"/> event.
/// </summary>
public sealed class CastDeviceFoundEventArgs : EventArgs
{
    /// <summary>Gets the device that was discovered.</summary>
    public ICastDevice Device { get; }

    /// <summary>Initialises a new instance of <see cref="CastDeviceFoundEventArgs"/>.</summary>
    /// <param name="device">The newly discovered cast device.</param>
    public CastDeviceFoundEventArgs(ICastDevice device)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
    }
}

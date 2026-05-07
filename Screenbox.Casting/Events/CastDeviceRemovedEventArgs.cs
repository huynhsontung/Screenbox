#nullable enable

using Screenbox.Casting.Abstractions;
using System;

namespace Screenbox.Casting.Events;

/// <summary>
/// Provides data for the <see cref="ICastDeviceLocator.DeviceLost"/> event.
/// </summary>
public sealed class CastDeviceRemovedEventArgs : EventArgs
{
    /// <summary>Gets the device that is no longer reachable.</summary>
    public ICastDevice Device { get; }

    /// <summary>Initialises a new instance of <see cref="CastDeviceRemovedEventArgs"/>.</summary>
    /// <param name="device">The cast device that became unavailable.</param>
    public CastDeviceRemovedEventArgs(ICastDevice device)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
    }
}

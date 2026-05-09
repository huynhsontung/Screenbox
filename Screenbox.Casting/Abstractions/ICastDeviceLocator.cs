#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Casting.Events;

namespace Screenbox.Casting.Abstractions;

/// <summary>
/// Discovers cast-capable devices on the local network and creates sessions for them.
/// </summary>
/// <remarks>
/// Each protocol (Chromecast, DLNA) has its own locator implementation.
/// A single locator instance should be active only while the user has the cast picker open;
/// call <see cref="Stop"/> and <see cref="Dispose"/> when discovery is no longer needed.
/// </remarks>
public interface ICastDeviceLocator : IDisposable
{
    /// <summary>Raised when a new device is discovered on the network.</summary>
    event EventHandler<CastDeviceFoundEventArgs>? DeviceFound;

    /// <summary>Raised when a previously discovered device is no longer reachable.</summary>
    event EventHandler<CastDeviceRemovedEventArgs>? DeviceLost;

    /// <summary>Gets a value indicating whether discovery is currently running.</summary>
    bool IsStarted { get; }

    /// <summary>Returns a snapshot of all currently known devices.</summary>
    IReadOnlyList<ICastDevice> Devices { get; }

    /// <summary>
    /// Starts continuous device discovery.
    /// </summary>
    /// <returns><c>true</c> if discovery was started; <c>false</c> if it was already running.</returns>
    bool Start();

    /// <summary>
    /// Stops discovery and raises <see cref="DeviceLost"/> for all known devices.
    /// </summary>
    void Stop();

    /// <summary>
    /// Connects to the specified device, sets the media URL, and starts playback at
    /// <paramref name="startPosition"/>.
    /// </summary>
    /// <param name="device">
    /// A device returned by this locator's <see cref="DeviceFound"/> event.
    /// Must be an instance created by this locator.
    /// </param>
    /// <param name="streamUrl">
    /// The HTTP URL from which the cast device should fetch the media.
    /// For local files this is the URL served by the local streaming HTTP server.
    /// </param>
    /// <param name="startPosition">Position at which playback should begin.</param>
    /// <returns>
    /// The active <see cref="ICastSession"/> when the connection succeeds; otherwise <c>null</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// ICastSession? session = await locator.ConnectAsync(device, streamUrl, TimeSpan.Zero);
    /// if (session is not null)
    ///     await session.PlayAsync();
    /// </code>
    /// </example>
    Task<ICastSession?> ConnectAsync(ICastDevice device, Uri streamUrl, TimeSpan startPosition);
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Provides protocol-agnostic media casting functionality by bridging <see cref="ICastDeviceLocator"/>
/// implementations with the local media streaming infrastructure.
/// </summary>
/// <remarks>
/// The service creates one locator per supported protocol (Chromecast, DLNA) and delegates
/// connection to the appropriate locator based on the target <see cref="ICastDevice"/>.
/// Play/pause/seek/volume controls are on <see cref="ICastSession"/> directly.
/// </remarks>
public interface ICastService
{
    /// <summary>
    /// Creates a fresh set of device locators — one for each supported casting protocol
    /// (Chromecast and DLNA/UPnP).
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="ICastDeviceLocator"/> instances, one per protocol.
    /// The caller is responsible for starting, stopping, and disposing the locators.
    /// </returns>
    IReadOnlyList<ICastDeviceLocator> CreateLocators();

    /// <summary>
    /// Resolves a streamable HTTP URL for <paramref name="item"/>, then instructs the
    /// appropriate protocol locator to connect to <paramref name="device"/> and start playback.
    /// </summary>
    /// <param name="device">The target cast device.</param>
    /// <param name="item">The media to cast.</param>
    /// <param name="startPosition">The position at which playback should begin.</param>
    /// <returns>
    /// An <see cref="ICastSession"/> on success; <c>null</c> if the device cannot be reached
    /// or the stream URL cannot be resolved.
    /// </returns>
    Task<ICastSession?> ConnectAndCastAsync(ICastDevice device, PlaybackItem item, TimeSpan startPosition);

    /// <summary>
    /// Stops the active cast session, signals the remote device to stop, and tears down
    /// any local HTTP media stream.
    /// </summary>
    /// <param name="session">The session to disconnect; may be <c>null</c> (no-op).</param>
    Task DisconnectAsync(ICastSession? session = null);
}


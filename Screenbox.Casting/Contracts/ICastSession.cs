#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Casting.Models;

namespace Screenbox.Casting.Contracts;

/// <summary>
/// Cast session contract for a connected device.
/// </summary>
public interface ICastSession : IAsyncDisposable
{
    CastSessionState State { get; }

    CastDevice? Device { get; }

    event EventHandler<CastSessionState>? StateChanged;

    /// <summary>
    /// Connects to the target device over Cast v2 transport.
    /// </summary>
    Task ConnectAsync(CastDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Launches the default media receiver app.
    /// </summary>
    Task LaunchDefaultReceiverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads media into receiver app.
    /// </summary>
    Task<CastCompatibilityResult> LoadAsync(CastMediaSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends play command.
    /// </summary>
    Task PlayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends pause command.
    /// </summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends stop command.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects the cast session.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

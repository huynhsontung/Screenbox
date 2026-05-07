#nullable enable

using System;
using System.Threading.Tasks;

namespace Screenbox.Casting.Abstractions;

/// <summary>
/// Represents an active cast session with a remote device, providing playback controls
/// and observable state.
/// </summary>
/// <remarks>
/// Dispose the session to release network resources.  The session raises
/// <see cref="Disconnected"/> automatically if the device drops the connection.
/// </remarks>
public interface ICastSession : IDisposable
{
    /// <summary>Gets the device this session is connected to.</summary>
    ICastDevice Device { get; }

    /// <summary>
    /// Gets the current playback position reported by the remote device, in seconds.
    /// Updated asynchronously from status events (Chromecast) or polling (DLNA).
    /// </summary>
    double Position { get; }

    /// <summary>
    /// Gets the duration of the currently-loaded media, in seconds.
    /// Remains zero until the remote device reports a valid duration.
    /// </summary>
    double Duration { get; }

    /// <summary>Gets a value indicating whether the remote device is actively playing.</summary>
    bool IsPlaying { get; }

    /// <summary>Gets a value indicating whether the remote device is buffering media.</summary>
    bool IsBuffering { get; }

    /// <summary>
    /// Gets the current receiver volume level, as a value between 0.0 (silent)
    /// and 1.0 (full volume).
    /// </summary>
    double Volume { get; }

    /// <summary>Gets a value indicating whether the receiver is muted.</summary>
    bool IsMuted { get; }

    /// <summary>
    /// Raised when the remote device finishes playback naturally (track ended, error,
    /// or cancelled by the device) — not when the user explicitly calls <see cref="StopAsync"/>.
    /// Always raised on the UI thread.
    /// </summary>
    event EventHandler? PlaybackEnded;

    /// <summary>
    /// Raised when the connection to the remote device is lost unexpectedly.
    /// Always raised on the UI thread.
    /// </summary>
    event EventHandler? Disconnected;

    /// <summary>Sends a play command to resume playback.</summary>
    Task PlayAsync();

    /// <summary>Sends a pause command to suspend playback.</summary>
    Task PauseAsync();

    /// <summary>Sends a stop command and ends the media session on the device.</summary>
    Task StopAsync();

    /// <summary>Seeks the remote device to the specified playback position.</summary>
    /// <param name="position">The target position.</param>
    Task SeekAsync(TimeSpan position);

    /// <summary>
    /// Sets the receiver volume.
    /// </summary>
    /// <param name="level">A value between 0.0 and 1.0.</param>
    Task SetVolumeAsync(double level);

    /// <summary>Mutes or unmutes the receiver.</summary>
    /// <param name="muted">Pass <c>true</c> to mute, <c>false</c> to unmute.</param>
    Task SetMuteAsync(bool muted);
}

#nullable enable

using System;
using System.Threading.Tasks;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Provides Chromecast discovery and media casting functionality.
/// </summary>
/// <remarks>
/// Unlike the previous LibVLC-based implementation (which routed the decoded output stream
/// directly to a renderer), this interface hands a media URL to the Chromecast device, which
/// then fetches and plays the content independently.
/// </remarks>
public interface ICastService
{
    /// <summary>Creates a new <see cref="RendererWatcher"/> that discovers Chromecast devices on the local network.</summary>
    RendererWatcher CreateRendererWatcher();

    /// <summary>
    /// Connects to the specified renderer, resolves a streamable URL for the given playback
    /// item, and instructs the Chromecast to start playback at <paramref name="startPosition"/>.
    /// </summary>
    /// <param name="renderer">The target Chromecast device.</param>
    /// <param name="item">The media to cast.</param>
    /// <param name="startPosition">The position at which playback should begin.</param>
    /// <returns><c>true</c> if the cast was started successfully; otherwise <c>false</c>.</returns>
    Task<bool> ConnectAndCastAsync(Renderer renderer, PlaybackItem item, TimeSpan startPosition);

    /// <summary>Stops the active cast session, disconnects from the device, and stops any local HTTP stream.</summary>
    Task StopCastingAsync();
}


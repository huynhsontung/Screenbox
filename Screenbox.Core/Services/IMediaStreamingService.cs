#nullable enable

using System;
using System.Threading.Tasks;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Exposes local media files as HTTP streams so that Chromecast devices can fetch them.
/// For network URIs that the Chromecast can already reach (http/https), the original URI
/// is returned directly without starting a local HTTP server.
/// </summary>
public interface IMediaStreamingService
{
    /// <summary>
    /// Starts serving the given playback item over HTTP and returns the URL the Chromecast should use.
    /// </summary>
    /// <param name="item">The playback item to stream.</param>
    /// <returns>
    /// The URL the Chromecast should load, or <c>null</c> if the source cannot be served
    /// (e.g., the source type is not supported).
    /// </returns>
    /// <example>
    /// <code>
    /// Uri? url = await _streamingService.StartStreamAsync(playbackItem);
    /// if (url is not null)
    ///     await _client.MediaChannel.LoadAsync(new Media { ContentUrl = url.ToString() });
    /// </code>
    /// </example>
    Task<Uri?> StartStreamAsync(PlaybackItem item);

    /// <summary>Stops the active HTTP stream and releases all server resources.</summary>
    void StopStream();
}

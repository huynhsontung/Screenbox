#nullable enable

using System;
using System.Threading.Tasks;

namespace Screenbox.Casting.Abstractions;

/// <summary>
/// Exposes media sources as HTTP streams so cast devices can fetch them.
/// For network URIs that a cast device can already reach (http/https), the original URI
/// is returned directly without starting a local HTTP server.
/// </summary>
public interface IMediaStreamingService
{
    /// <summary>
    /// Starts serving the given media source over HTTP and returns the URL a cast device should use.
    /// </summary>
    /// <param name="source">The source object to stream (e.g., a local file or URI).</param>
    /// <returns>
    /// The URL the cast device should load, or <c>null</c> if the source cannot be served
    /// (e.g., the source type is not supported).
    /// </returns>
    Task<Uri?> StartStreamAsync(object source);

    /// <summary>Stops the active HTTP stream and releases all server resources.</summary>
    void StopStream();
}

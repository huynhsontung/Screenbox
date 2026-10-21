using System;

namespace Screenbox.Core.MediaSources;

/// <summary>
/// Represents a media source addressable via a URI (e.g., HTTP, HTTPS, RTSP).
/// </summary>
public interface IUriMediaSource : IMediaSource
{
    /// <summary>
    /// Gets the absolute URI of the media source.
    /// </summary>
    Uri Uri { get; }
}

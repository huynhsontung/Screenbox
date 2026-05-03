#nullable enable

using System;

namespace Screenbox.Casting.Models;

/// <summary>
/// Describes media metadata and URL used for Chromecast LOAD.
/// </summary>
public sealed class CastMediaSource
{
    /// <summary>
    /// Initializes a new <see cref="CastMediaSource"/> instance.
    /// </summary>
    public CastMediaSource(Uri contentUri, string contentType, string title, bool isLive = false, string? posterUri = null)
    {
        ContentUri = contentUri;
        ContentType = contentType;
        Title = title;
        IsLive = isLive;
        PosterUri = posterUri;
    }

    public Uri ContentUri { get; }

    public string ContentType { get; }

    public string Title { get; }

    public bool IsLive { get; }

    public string? PosterUri { get; }
}

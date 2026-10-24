using System;
using System.Linq;
using Screenbox.Core.Enums;

namespace Screenbox.Core.MediaSources;

public class UriMediaSource : IUriMediaSource
{
    public Uri Uri { get; }

    public string Location => Uri.OriginalString;

    public string Name { get; }

    public MediaPlaybackType MediaType { get; }

    public UriMediaSource(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        Uri = uri;
        Name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        MediaType = MediaPlaybackType.Unknown;
    }

    public override string ToString() => Location;
}

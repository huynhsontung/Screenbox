#nullable enable

using System;

namespace Screenbox.Core.Models;

public sealed record class MusicCacheRecordDto
{
    public string Path { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset DateAdded { get; set; }

    public TimeSpan Duration { get; set; }

    public uint Year { get; set; }

    public string Artist { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    public string AlbumArtist { get; set; } = string.Empty;

    public string Composers { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public uint TrackNumber { get; set; }

    public uint Bitrate { get; set; }
}

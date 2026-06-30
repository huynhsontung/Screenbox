#nullable enable

using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

public sealed class RawMediaRecordDto
{
    public string Path { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public MediaPlaybackType MediaType { get; set; }

    public long DateAddedTicks { get; set; }

    public long DurationTicks { get; set; }

    public uint Year { get; set; }

    public string Artist { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    public string AlbumArtist { get; set; } = string.Empty;

    public string Composers { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public uint TrackNumber { get; set; }

    public uint Bitrate { get; set; }

    public string Subtitle { get; set; } = string.Empty;

    public string Producers { get; set; } = string.Empty;

    public string Writers { get; set; } = string.Empty;

    public uint Width { get; set; }

    public uint Height { get; set; }

    public uint VideoBitrate { get; set; }
}

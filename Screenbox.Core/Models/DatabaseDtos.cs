#nullable enable

using System;
using System.Collections.Generic;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

internal sealed class RawCacheLoadResultDto
{
    public List<string> FolderPaths { get; init; } = new();

    public List<RawMediaRecordDto> Records { get; init; } = new();
}

internal sealed class RawMediaRecordDto
{
    public string Path { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public MediaPlaybackType MediaType { get; init; }

    public long DateAddedTicks { get; init; }

    public long DurationTicks { get; init; }

    public uint Year { get; init; }

    public string Artist { get; init; } = string.Empty;

    public string Album { get; init; } = string.Empty;

    public string AlbumArtist { get; init; } = string.Empty;

    public string Composers { get; init; } = string.Empty;

    public string Genre { get; init; } = string.Empty;

    public uint TrackNumber { get; init; }

    public uint Bitrate { get; init; }

    public string Subtitle { get; init; } = string.Empty;

    public string Producers { get; init; } = string.Empty;

    public string Writers { get; init; } = string.Empty;

    public uint Width { get; init; }

    public uint Height { get; init; }

    public uint VideoBitrate { get; init; }
}

internal sealed class MusicCacheRecordDto
{
    public string Path { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTimeOffset DateAdded { get; init; }

    public MusicInfo Info { get; init; } = new();
}

internal sealed class VideoCacheRecordDto
{
    public string Path { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTimeOffset DateAdded { get; init; }

    public VideoInfo Info { get; init; } = new();
}

internal sealed class SqlParameterDto
{
    public string Name { get; init; } = string.Empty;

    public object Value { get; init; } = DBNull.Value;
}

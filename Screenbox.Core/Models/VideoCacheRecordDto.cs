#nullable enable

using System;

namespace Screenbox.Core.Models;

public sealed record class VideoCacheRecordDto
{
    public string Path { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset DateAdded { get; set; }

    public TimeSpan Duration { get; set; }

    public uint Year { get; set; }

    public string Subtitle { get; set; } = string.Empty;

    public string Producers { get; set; } = string.Empty;

    public string Writers { get; set; } = string.Empty;

    public uint Width { get; set; }

    public uint Height { get; set; }

    public uint VideoBitrate { get; set; }
}

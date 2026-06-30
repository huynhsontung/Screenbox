#nullable enable

using System;

namespace Screenbox.Core.Models;

internal sealed class VideoCacheRecordDto
{
    public string Path { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset DateAdded { get; set; }

    public VideoInfo Info { get; set; } = new();
}

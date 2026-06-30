#nullable enable

using System;

namespace Screenbox.Core.Models;

internal sealed class MusicCacheRecordDto
{
    public string Path { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset DateAdded { get; set; }

    public MusicInfo Info { get; set; } = new();
}

#nullable enable

using System;
using System.Collections.Generic;

namespace Screenbox.Core.Models;

public sealed class PlaylistRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset LastUpdated { get; set; }

    public List<RawMediaRecordDto> Items { get; set; } = new();
}

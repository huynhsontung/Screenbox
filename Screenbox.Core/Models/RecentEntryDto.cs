using System;
using System.Collections.Generic;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

/// <summary>
/// Data transfer object representing a persisted recent media or playlist entry in SQLite.
/// </summary>
public sealed class RecentEntryDto
{
    public string Id { get; set; } = string.Empty;

    public RecentItemKind Kind { get; set; }

    public string SourceId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public int InitialIndex { get; set; }

    public DateTimeOffset LastPlayed { get; set; }

    public List<RawMediaRecordDto> Items { get; set; } = [];
}

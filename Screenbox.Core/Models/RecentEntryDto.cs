using System;
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

    public string? Token { get; set; }

    public DateTimeOffset LastPlayed { get; set; }

    /// <summary>
    /// Optional cached library metadata joined from <c>media_records</c> on load for <see cref="RecentItemKind.SingleMedia"/> entries.
    /// </summary>
    public RawMediaRecordDto? MediaRecord { get; set; }
}

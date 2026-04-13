#nullable enable

using System;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Data;

/// <summary>
/// Flat entity for cached media library records stored in the SQLite database.
/// One row per media file per library type.
/// </summary>
internal class MediaRecordEntity
{
    public int Id { get; set; }

    /// <summary>Whether this record belongs to the Music or Video library.</summary>
    public MediaPlaybackType LibraryType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public long DurationTicks { get; set; }

    public uint Year { get; set; }

    public MediaPlaybackType MediaType { get; set; }

    /// <summary>UTC date/time the file was added to the library.</summary>
    public DateTime DateAddedUtc { get; set; }

    // ── Music-only columns ──────────────────────────────────────────────────

    public string? Artist { get; set; }

    public string? Album { get; set; }

    public string? AlbumArtist { get; set; }

    public string? Composers { get; set; }

    public string? Genre { get; set; }

    public uint? TrackNumber { get; set; }

    public uint? MusicBitrate { get; set; }

    // ── Video-only columns ──────────────────────────────────────────────────

    public string? VideoSubtitle { get; set; }

    public string? Producers { get; set; }

    public string? Writers { get; set; }

    public uint? Width { get; set; }

    public uint? Height { get; set; }

    public uint? VideoBitrate { get; set; }
}

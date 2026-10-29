using System;

namespace Screenbox.Core.MediaSources;

/// <summary>
/// Represents a child media source produced by expanding a playlist or container,
/// with optional pre-parsed metadata (such as from #EXTINF directives).
/// </summary>
public sealed record ExpandedMediaItem(
    IMediaSource Source,
    string? Title = null,
    TimeSpan? Duration = null);

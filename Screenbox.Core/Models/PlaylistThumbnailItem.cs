#nullable enable

using Screenbox.Core.MediaSources;

namespace Screenbox.Core.Models;

/// <summary>
/// Describes a media source candidate for playlist thumbnail collage generation.
/// </summary>
/// <param name="Source">The underlying media source used to extract thumbnail artwork.</param>
/// <param name="GroupKey">
/// Optional deduplication key (such as an album key). Once an item in a group supplies readable artwork,
/// subsequent items with the same key are skipped. When <see langword="null"/>, <see cref="IMediaSource.Location"/> is used.
/// </param>
public sealed record PlaylistThumbnailItem(
    IMediaSource Source,
    string? GroupKey = null);

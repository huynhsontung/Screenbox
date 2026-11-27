using System;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

/// <summary>
/// Extracts media metadata from an <see cref="IMediaSource"/>.
/// </summary>
public interface ISourceMetadataService
{
    /// <summary>
    /// Retrieves <see cref="MediaInfo"/> from the specified media source.
    /// </summary>
    /// <param name="source">The media source to inspect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The extracted <see cref="MediaInfo"/> if successful; otherwise, <see langword="null"/>.</returns>
    Task<MediaInfo?> GetMediaInfoAsync(IMediaSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves lightweight display summary properties (artist and duration) for the specified media source.
    /// </summary>
    /// <param name="source">The media source to inspect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The extracted artist name (if any) and duration.</returns>
    Task<(string? Artist, TimeSpan Duration)> GetSummaryPropertiesAsync(
        IMediaSource source,
        CancellationToken cancellationToken = default);
}

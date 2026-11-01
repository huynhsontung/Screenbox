using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Provides on-demand media metadata and track probing independent of a specific media library implementation.
/// </summary>
public interface IMediaProbeService
{
    /// <summary>
    /// Probes the specified media source and extracts its <see cref="MediaInfo"/>.
    /// </summary>
    /// <param name="source">The media source to probe.</param>
    /// <param name="options">Optional media initialization options.</param>
    /// <param name="cancellationToken">A token to cancel the probe operation.</param>
    /// <returns>The parsed <see cref="MediaInfo"/> if probing succeeded; otherwise, <see langword="null"/>.</returns>
    Task<MediaInfo?> ProbeMediaAsync(IMediaSource source, string[]? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Probes the specified playback item and extracts its <see cref="MediaInfo"/>.
    /// </summary>
    /// <param name="item">The playback item to probe.</param>
    /// <param name="cancellationToken">A token to cancel the probe operation.</param>
    /// <returns>The parsed <see cref="MediaInfo"/> if probing succeeded; otherwise, <see langword="null"/>.</returns>
    Task<MediaInfo?> ProbeMediaAsync(IPlaybackItem item, CancellationToken cancellationToken = default);
}

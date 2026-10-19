using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

/// <summary>
/// Provides on-demand media metadata and track probing independent of a specific media library implementation.
/// </summary>
public interface IMediaProbeService
{
    /// <summary>
    /// Probes the specified media source and extracts its <see cref="MediaInfo"/>.
    /// </summary>
    /// <param name="source">The media source to probe (e.g., <see cref="Windows.Storage.StorageFile"/>, <see cref="System.Uri"/>, or <see cref="Playback.PlaybackItem"/>).</param>
    /// <param name="options">Optional media initialization options.</param>
    /// <param name="cancellationToken">A token to cancel the probe operation.</param>
    /// <returns>The parsed <see cref="MediaInfo"/> if probing succeeded; otherwise, <see langword="null"/>.</returns>
    Task<MediaInfo?> ProbeMediaAsync(object source, string[]? options = null, CancellationToken cancellationToken = default);
}

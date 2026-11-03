using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Extracts media and track metadata from an <see cref="IPlaybackItem"/>.
/// </summary>
public interface IPlaybackMetadataService
{
    /// <summary>
    /// Retrieves <see cref="MediaInfo"/> from the specified playback item.
    /// </summary>
    /// <param name="item">The playback item to inspect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The extracted <see cref="MediaInfo"/> if successful; otherwise, <see langword="null"/>.</returns>
    Task<MediaInfo?> GetMediaInfoAsync(IPlaybackItem item, CancellationToken cancellationToken = default);
}

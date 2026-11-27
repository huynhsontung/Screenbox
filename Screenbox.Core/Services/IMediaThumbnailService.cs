using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Windows.Storage.Streams;

namespace Screenbox.Core.Services;

/// <summary>
/// Extracts thumbnail image streams from an <see cref="IMediaSource"/>.
/// </summary>
public interface IMediaThumbnailService
{
    /// <summary>
    /// Retrieves a thumbnail image stream for the specified media <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The media source to inspect.</param>
    /// <param name="requestedSize">The requested thumbnail dimension in pixels.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IRandomAccessStream"/> containing the thumbnail image if available;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    Task<IRandomAccessStream?> GetThumbnailStreamAsync(
        IMediaSource source,
        uint requestedSize = 1280,
        CancellationToken cancellationToken = default);
}

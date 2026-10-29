using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Screenbox.Core.MediaSources;

/// <summary>
/// Represents a media source that is a container or playlist manifest rather than
/// a directly playable stream, and must be expanded before playback.
/// </summary>
public interface IExpandableMediaSource : IMediaSource
{
    /// <summary>
    /// Expands the container into its child media sources.
    /// </summary>
    Task<IReadOnlyList<ExpandedMediaItem>> ExpandAsync(CancellationToken cancellationToken = default);
}

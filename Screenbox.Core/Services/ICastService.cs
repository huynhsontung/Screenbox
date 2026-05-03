#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

public interface ICastService
{
    /// <summary>
    /// Create a new renderer watcher for the specified media player
    /// </summary>
    RendererWatcher CreateRendererWatcher(IMediaPlayer player);

    /// <summary>
    /// Set the active renderer for the media player
    /// </summary>
    Task<CastOperationResult> SetActiveRendererAsync(CastContext context, IMediaPlayer player, Renderer? renderer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a play command to the active cast session.
    /// </summary>
    Task<CastOperationResult> PlayAsync(CastContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a pause command to the active cast session.
    /// </summary>
    Task<CastOperationResult> PauseAsync(CastContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a stop command to the active cast session.
    /// </summary>
    Task<CastOperationResult> StopAsync(CastContext context, CancellationToken cancellationToken = default);
}

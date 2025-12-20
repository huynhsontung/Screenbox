#nullable enable

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
    bool SetActiveRenderer(IMediaPlayer player, Renderer? renderer);
}

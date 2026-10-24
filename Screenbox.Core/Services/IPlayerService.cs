using Screenbox.Core.MediaSources;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

public interface IPlayerService
{
    IMediaPlayer Initialize(string[] swapChainOptions);

    IPlaybackItem CreatePlaybackItem(IMediaPlayer player, IMediaSource source, params string[] options);

    void DisposePlayer(IMediaPlayer player);
}

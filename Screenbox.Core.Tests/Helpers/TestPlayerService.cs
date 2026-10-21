using Screenbox.Core.MediaSources;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Helpers;

public class TestPlayerService : IPlayerService
{
    public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

    public IPlaybackItem CreatePlaybackItem(IMediaPlayer player, IMediaSource source, params string[] options) => throw new NotImplementedException();

    public void DisposePlaybackItem(IPlaybackItem item) { }

    public void DisposePlayer(IMediaPlayer player) { }
}

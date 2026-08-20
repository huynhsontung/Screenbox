using Screenbox.Core.Playback;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Helpers;

public class TestPlayerService : IPlayerService
{
    public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

    public PlaybackItem CreatePlaybackItem(IMediaPlayer player, object source, params string[] options) => throw new NotImplementedException();

    public void DisposePlaybackItem(PlaybackItem item) { }

    public void DisposePlayer(IMediaPlayer player) { }
}

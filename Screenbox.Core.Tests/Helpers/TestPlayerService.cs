using System;
using System.Runtime.CompilerServices;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Helpers;

public class TestPlayerService : IPlayerService
{
    private Action<MediaInfo, Uri?>? _onMediaParsed;

    public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

    public PlaybackItem CreatePlaybackItem(IMediaPlayer player, object source, params string[] options) =>
        (PlaybackItem)RuntimeHelpers.GetUninitializedObject(typeof(PlaybackItem));

    public void WatchPlaybackItem(PlaybackItem item, Action<MediaInfo, Uri?> onMediaParsed)
    {
        _onMediaParsed = onMediaParsed;
    }

    public void SimulateMediaParsed(MediaInfo mediaInfo, Uri? artworkUri = null)
    {
        _onMediaParsed?.Invoke(mediaInfo, artworkUri);
    }

    public void DisposePlaybackItem(PlaybackItem item)
    {
        _onMediaParsed = null;
    }

    public void DisposePlayer(IMediaPlayer player) { }
}

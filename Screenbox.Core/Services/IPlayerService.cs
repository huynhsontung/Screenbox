using System;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

public interface IPlayerService
{
    IMediaPlayer Initialize(string[] swapChainOptions);

    PlaybackItem CreatePlaybackItem(IMediaPlayer player, object source, params string[] options);

    void WatchPlaybackItem(PlaybackItem item, Action<MediaInfo, Uri?> onMediaParsed);

    void DisposePlaybackItem(PlaybackItem item);

    void DisposePlayer(IMediaPlayer player);
}

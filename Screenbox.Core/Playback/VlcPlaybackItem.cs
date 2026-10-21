using System;
using LibVLCSharp.Shared;
using Screenbox.Core.MediaSources;

namespace Screenbox.Core.Playback;

public class VlcPlaybackItem : PlaybackItem
{
    public VlcPlaybackItem(IMediaSource source, Media media, Action<Media>? disposeCallback = null, IMediaSourceFactory? sourceFactory = null)
        : base(source, media, disposeCallback, sourceFactory)
    {
    }
}

#nullable enable

using System;
using Windows.Media;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;
public sealed class MediaInfo
{
    public MediaPlaybackType MediaType { get; set; }

    public VideoInfo VideoProperties { get; set; }

    public MusicInfo MusicProperties { get; set; }

    public ulong Size { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public MediaInfo()
    {
        VideoProperties = new VideoInfo();
        MusicProperties = new MusicInfo();
    }

    public MediaInfo(MusicInfo musicProperties)
    {
        MediaType = MediaPlaybackType.Music;
        MusicProperties = musicProperties;
        VideoProperties = new VideoInfo();
    }

    public MediaInfo(VideoInfo videoProperties)
    {
        MediaType = MediaPlaybackType.Video;
        MusicProperties = new MusicInfo();
        VideoProperties = videoProperties;
    }

    public MediaInfo(BasicProperties basicProperties, MusicProperties musicProperties)
    {
        Size = basicProperties.Size;
        DateModified = basicProperties.DateModified;
        MediaType = MediaPlaybackType.Music;
        MusicProperties = new MusicInfo(musicProperties);
        VideoProperties = new VideoInfo();
    }

    public MediaInfo(BasicProperties basicProperties, VideoProperties videoProperties)
    {
        Size = basicProperties.Size;
        DateModified = basicProperties.DateModified;
        MediaType = MediaPlaybackType.Video;
        MusicProperties = new MusicInfo();
        VideoProperties = new VideoInfo(videoProperties);
    }
}

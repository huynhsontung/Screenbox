#nullable enable

using Screenbox.Core.Enums;
using System;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;
public sealed class MediaInfo
{
    public MediaPlaybackType MediaType { get; set; }

    public VideoInfo VideoProperties { get; }

    public MusicInfo MusicProperties { get; }

    public ulong Size { get; }

    public DateTimeOffset DateModified { get; }

    public MediaInfo(MediaPlaybackType mediaType)
    {
        MediaType = mediaType;
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

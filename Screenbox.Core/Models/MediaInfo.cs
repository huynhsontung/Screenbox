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

    internal MediaInfo(IMediaProperties properties)
    {
        MediaType = MediaPlaybackType.Music;
        if (properties is MusicInfo musicProperties)
        {
            MusicProperties = musicProperties;
            VideoProperties = new VideoInfo();
        }
        else if (properties is VideoInfo videoProperties)
        {
            MusicProperties = new MusicInfo();
            VideoProperties = videoProperties;
        }
        else
        {
            throw new ArgumentException("Invalid media properties type.");
        }
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

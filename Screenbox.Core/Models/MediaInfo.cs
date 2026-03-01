#nullable enable

using System;
using Screenbox.Core.Enums;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;

public sealed class MediaInfo
{
    public MediaPlaybackType MediaType { get; set; }

    public VideoInfo VideoProperties { get; }

    public MusicInfo MusicProperties { get; }

    public ulong Size { get; }

    public DateTimeOffset DateModified { get; }

    public MediaInfo(MediaPlaybackType mediaType, string title = "", uint year = default, TimeSpan duration = default)
    {
        MediaType = mediaType;
        VideoProperties = new VideoInfo();
        MusicProperties = new MusicInfo();

        VideoProperties.Title = title;
        VideoProperties.Duration = duration;
        VideoProperties.Year = year;
        MusicProperties.Title = title;
        MusicProperties.Duration = duration;
        MusicProperties.Year = year;
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

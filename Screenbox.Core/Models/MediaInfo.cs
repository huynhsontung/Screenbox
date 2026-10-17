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
        MediaType = MediaPlaybackType.Unknown;
        if (properties is MusicInfo musicProperties)
        {
            MediaType = MediaPlaybackType.Music;
            MusicProperties = musicProperties;
            VideoProperties = new VideoInfo();
        }
        else if (properties is VideoInfo videoProperties)
        {
            MediaType = MediaPlaybackType.Video;
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

    public MediaInfo(
        MediaPlaybackType mediaType,
        MusicInfo musicProperties,
        VideoInfo videoProperties,
        ulong size = default,
        DateTimeOffset dateModified = default)
    {
        MediaType = mediaType;
        MusicProperties = musicProperties;
        VideoProperties = videoProperties;
        Size = size;
        DateModified = dateModified;
    }

    /// <summary>
    /// Merges two <see cref="MediaInfo"/> instances into a new <see cref="MediaInfo"/> instance,
    /// preferring non-empty and non-zero values from <paramref name="preferred"/> while falling back to <paramref name="baseInfo"/>.
    /// </summary>
    /// <param name="baseInfo">The fallback media information (e.g., from WinRT file properties or initial defaults).</param>
    /// <param name="preferred">The preferred media information (e.g., from media probe or parser).</param>
    /// <returns>A new <see cref="MediaInfo"/> instance containing the merged properties.</returns>
    public static MediaInfo Merge(MediaInfo baseInfo, MediaInfo preferred)
    {
        MediaPlaybackType mediaType = baseInfo.MediaType != MediaPlaybackType.Unknown
            ? baseInfo.MediaType
            : preferred.MediaType;
        ulong size = preferred.Size > 0 ? preferred.Size : baseInfo.Size;
        DateTimeOffset dateModified = preferred.DateModified != default ? preferred.DateModified : baseInfo.DateModified;
        MusicInfo musicInfo = MusicInfo.Merge(baseInfo.MusicProperties, preferred.MusicProperties);
        VideoInfo videoInfo = VideoInfo.Merge(baseInfo.VideoProperties, preferred.VideoProperties);

        return new MediaInfo(mediaType, musicInfo, videoInfo, size, dateModified);
    }
}

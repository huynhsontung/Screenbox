using System;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;

public sealed class VideoInfo : IMediaProperties
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Producers { get; set; } = string.Empty;
    public string Writers { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public uint Year { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint Bitrate { get; set; }

    /** VLC metadata **/
    public string ShowName { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public string Episode { get; set; } = string.Empty;

    public VideoInfo() { }

    public VideoInfo(VideoProperties videoProperties)
    {
        Title = videoProperties.Title;
        Subtitle = videoProperties.Subtitle;
        Year = videoProperties.Year;
        Producers = string.Join(", ", videoProperties.Producers);
        Writers = string.Join(", ", videoProperties.Writers);
        Duration = videoProperties.Duration;
        Width = videoProperties.Width;
        Height = videoProperties.Height;
        Bitrate = videoProperties.Bitrate;
    }

    /// <summary>
    /// Merges two <see cref="VideoInfo"/> instances into a new <see cref="VideoInfo"/> instance,
    /// preferring non-empty and non-zero values from <paramref name="preferred"/> while falling back to <paramref name="baseInfo"/>.
    /// </summary>
    /// <param name="baseInfo">The fallback video properties.</param>
    /// <param name="preferred">The preferred video properties.</param>
    /// <returns>A new <see cref="VideoInfo"/> instance containing the merged properties.</returns>
    public static VideoInfo Merge(VideoInfo baseInfo, VideoInfo preferred)
    {
        return new VideoInfo
        {
            Title = !string.IsNullOrEmpty(preferred.Title) ? preferred.Title : baseInfo.Title,
            Subtitle = !string.IsNullOrEmpty(preferred.Subtitle) ? preferred.Subtitle : baseInfo.Subtitle,
            Producers = !string.IsNullOrEmpty(preferred.Producers) ? preferred.Producers : baseInfo.Producers,
            Writers = !string.IsNullOrEmpty(preferred.Writers) ? preferred.Writers : baseInfo.Writers,
            Duration = preferred.Duration > TimeSpan.Zero ? preferred.Duration : baseInfo.Duration,
            Year = preferred.Year > 0 ? preferred.Year : baseInfo.Year,
            Width = preferred.Width > 0 ? preferred.Width : baseInfo.Width,
            Height = preferred.Height > 0 ? preferred.Height : baseInfo.Height,
            Bitrate = preferred.Bitrate > 0 ? preferred.Bitrate : baseInfo.Bitrate,
            ShowName = !string.IsNullOrEmpty(preferred.ShowName) ? preferred.ShowName : baseInfo.ShowName,
            Season = !string.IsNullOrEmpty(preferred.Season) ? preferred.Season : baseInfo.Season,
            Episode = !string.IsNullOrEmpty(preferred.Episode) ? preferred.Episode : baseInfo.Episode
        };
    }
}
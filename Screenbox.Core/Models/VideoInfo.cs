using System;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;
public sealed record VideoInfo
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
}

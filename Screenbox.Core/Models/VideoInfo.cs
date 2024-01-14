using ProtoBuf;
using System;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;
[ProtoContract]
public sealed class VideoInfo
{
    [ProtoMember(1)] public string Title { get; set; } = string.Empty;
    [ProtoMember(2)] public string Subtitle { get; set; } = string.Empty;
    [ProtoMember(3)] public string Producers { get; set; } = string.Empty;
    [ProtoMember(4)] public string Writers { get; set; } = string.Empty;
    [ProtoMember(5)] public TimeSpan Duration { get; set; }
    [ProtoMember(6)] public uint Year { get; set; }
    [ProtoMember(7)] public uint Width { get; set; }
    [ProtoMember(8)] public uint Height { get; set; }
    [ProtoMember(9)] public uint Bitrate { get; set; }

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

#nullable enable

using System;
using Windows.Media;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;
public sealed record MediaInfo
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

public sealed record MusicInfo
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string AlbumArtist { get; set; } = string.Empty;
    public string Composers { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public uint TrackNumber { get; set; }
    public uint Year { get; set; }
    public TimeSpan Duration { get; set; }
    public uint Bitrate { get; set; }

    public MusicInfo() { }

    public MusicInfo(MusicProperties musicProperties)
    {
        Title = musicProperties.Title;
        Artist = musicProperties.Artist;
        Album = musicProperties.Album;
        AlbumArtist = musicProperties.AlbumArtist;
        Composers = string.Join(", ", musicProperties.Composers);
        Genre = string.Join(", ", musicProperties.Genre);
        TrackNumber = musicProperties.TrackNumber;
        Year = musicProperties.Year;
        Duration = musicProperties.Duration;
        Bitrate = musicProperties.Bitrate;
    }
}

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

using System;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;

public sealed class MusicInfo : IMediaProperties
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
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

    /// <summary>
    /// Merges two <see cref="MusicInfo"/> instances into a new <see cref="MusicInfo"/> instance,
    /// preferring non-empty and non-zero values from <paramref name="preferred"/> while falling back to <paramref name="baseInfo"/>.
    /// </summary>
    /// <param name="baseInfo">The fallback music properties.</param>
    /// <param name="preferred">The preferred music properties.</param>
    /// <returns>A new <see cref="MusicInfo"/> instance containing the merged properties.</returns>
    public static MusicInfo Merge(MusicInfo baseInfo, MusicInfo preferred)
    {
        return new MusicInfo
        {
            Title = !string.IsNullOrEmpty(preferred.Title) ? preferred.Title : baseInfo.Title,
            Artist = !string.IsNullOrEmpty(preferred.Artist) ? preferred.Artist : baseInfo.Artist,
            Album = !string.IsNullOrEmpty(preferred.Album) ? preferred.Album : baseInfo.Album,
            AlbumArtist = !string.IsNullOrEmpty(preferred.AlbumArtist) ? preferred.AlbumArtist : baseInfo.AlbumArtist,
            Composers = !string.IsNullOrEmpty(preferred.Composers) ? preferred.Composers : baseInfo.Composers,
            Genre = !string.IsNullOrEmpty(preferred.Genre) ? preferred.Genre : baseInfo.Genre,
            TrackNumber = preferred.TrackNumber > 0 ? preferred.TrackNumber : baseInfo.TrackNumber,
            Year = preferred.Year > 0 ? preferred.Year : baseInfo.Year,
            Duration = preferred.Duration > TimeSpan.Zero ? preferred.Duration : baseInfo.Duration,
            Bitrate = preferred.Bitrate > 0 ? preferred.Bitrate : baseInfo.Bitrate
        };
    }
}
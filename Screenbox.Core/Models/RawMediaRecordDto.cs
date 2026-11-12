using System;
using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

public sealed record class RawMediaRecordDto
{
    public string Path { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public MediaPlaybackType MediaType { get; set; }

    public DateTimeOffset DateAdded { get; set; }

    public TimeSpan Duration { get; set; }

    public uint Year { get; set; }

    public string Artist { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    public string AlbumArtist { get; set; } = string.Empty;

    public string Composers { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public uint TrackNumber { get; set; }

    public uint Bitrate { get; set; }

    public string Subtitle { get; set; } = string.Empty;

    public string Producers { get; set; } = string.Empty;

    public string Writers { get; set; } = string.Empty;

    public uint Width { get; set; }

    public uint Height { get; set; }

    public uint VideoBitrate { get; set; }

    public string? Token { get; set; }

    /// <summary>
    /// Creates a <see cref="RawMediaRecordDto"/> from a <see cref="MediaViewModel"/>.
    /// </summary>
    public static RawMediaRecordDto FromMediaViewModel(MediaViewModel media)
    {
        return new RawMediaRecordDto
        {
            Path = media.Location,
            Title = media.Name,
            MediaType = media.MediaType,
            DateAdded = media.DateAdded,
            Duration = media.Duration,
            Year = media.MediaType == MediaPlaybackType.Music
                ? media.MediaInfo.MusicProperties.Year
                : media.MediaInfo.VideoProperties.Year,
            Artist = media.MediaInfo.MusicProperties.Artist,
            Album = media.MediaInfo.MusicProperties.Album,
            AlbumArtist = media.MediaInfo.MusicProperties.AlbumArtist,
            Composers = media.MediaInfo.MusicProperties.Composers,
            Genre = media.MediaInfo.MusicProperties.Genre,
            TrackNumber = media.MediaInfo.MusicProperties.TrackNumber,
            Bitrate = media.MediaInfo.MusicProperties.Bitrate,
            Subtitle = media.MediaInfo.VideoProperties.Subtitle,
            Producers = media.MediaInfo.VideoProperties.Producers,
            Writers = media.MediaInfo.VideoProperties.Writers,
            Width = media.MediaInfo.VideoProperties.Width,
            Height = media.MediaInfo.VideoProperties.Height,
            VideoBitrate = media.MediaInfo.VideoProperties.Bitrate,
        };
    }

    /// <summary>
    /// Converts this record to a <see cref="MediaInfo"/> instance.
    /// </summary>
    public MediaInfo ToMediaInfo()
    {
        if (MediaType == MediaPlaybackType.Music)
        {
            return new MediaInfo(new MusicInfo
            {
                Title = Title,
                Artist = Artist,
                Album = Album,
                AlbumArtist = AlbumArtist,
                Composers = Composers,
                Genre = Genre,
                TrackNumber = TrackNumber,
                Year = Year,
                Duration = Duration,
                Bitrate = Bitrate,
            });
        }

        if (MediaType == MediaPlaybackType.Video)
        {
            return new MediaInfo(new VideoInfo
            {
                Title = Title,
                Subtitle = Subtitle,
                Producers = Producers,
                Writers = Writers,
                Year = Year,
                Duration = Duration,
                Width = Width,
                Height = Height,
                Bitrate = VideoBitrate,
            });
        }

        return new MediaInfo(MediaType, Title, Year, Duration);
    }
}

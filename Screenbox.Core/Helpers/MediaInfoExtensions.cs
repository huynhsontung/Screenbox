using System;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Provides extension methods for <see cref="MediaInfo"/> instances.
/// </summary>
public static class MediaInfoExtensions
{
    /// <summary>
    /// Returns a new <see cref="MediaInfo"/> instance where empty or default-value fields in <paramref name="target"/>
    /// are updated by non-empty / non-default values from <paramref name="source"/>, preserving any fields that were
    /// already populated in <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The base media info instance whose populated fields take precedence.</param>
    /// <param name="source">The incoming media info instance providing fallback values for empty/default fields.</param>
    /// <returns>A new <see cref="MediaInfo"/> containing the selectively merged properties.</returns>
    public static MediaInfo MergeSelective(this MediaInfo target, MediaInfo source)
    {
        MediaPlaybackType mediaType = target.MediaType != MediaPlaybackType.Unknown
            ? target.MediaType
            : source.MediaType;

        ulong size = target.Size > 0 ? target.Size : source.Size;
        DateTimeOffset dateModified = target.DateModified != default ? target.DateModified : source.DateModified;
        Uri? artworkUri = target.ArtworkUri ?? source.ArtworkUri;

        MusicInfo musicInfo = new()
        {
            Title = !string.IsNullOrEmpty(target.MusicProperties.Title)
                ? target.MusicProperties.Title
                : source.MusicProperties.Title,
            Artist = !string.IsNullOrEmpty(target.MusicProperties.Artist)
                ? target.MusicProperties.Artist
                : source.MusicProperties.Artist,
            Album = !string.IsNullOrEmpty(target.MusicProperties.Album)
                ? target.MusicProperties.Album
                : source.MusicProperties.Album,
            AlbumArtist = !string.IsNullOrEmpty(target.MusicProperties.AlbumArtist)
                ? target.MusicProperties.AlbumArtist
                : source.MusicProperties.AlbumArtist,
            Composers = !string.IsNullOrEmpty(target.MusicProperties.Composers)
                ? target.MusicProperties.Composers
                : source.MusicProperties.Composers,
            Genre = !string.IsNullOrEmpty(target.MusicProperties.Genre)
                ? target.MusicProperties.Genre
                : source.MusicProperties.Genre,
            TrackNumber = target.MusicProperties.TrackNumber > 0
                ? target.MusicProperties.TrackNumber
                : source.MusicProperties.TrackNumber,
            Year = target.MusicProperties.Year > 0
                ? target.MusicProperties.Year
                : source.MusicProperties.Year,
            Duration = target.MusicProperties.Duration > TimeSpan.Zero
                ? target.MusicProperties.Duration
                : source.MusicProperties.Duration,
            Bitrate = target.MusicProperties.Bitrate > 0
                ? target.MusicProperties.Bitrate
                : source.MusicProperties.Bitrate
        };

        VideoInfo videoInfo = new()
        {
            Title = !string.IsNullOrEmpty(target.VideoProperties.Title)
                ? target.VideoProperties.Title
                : source.VideoProperties.Title,
            Subtitle = !string.IsNullOrEmpty(target.VideoProperties.Subtitle)
                ? target.VideoProperties.Subtitle
                : source.VideoProperties.Subtitle,
            Producers = !string.IsNullOrEmpty(target.VideoProperties.Producers)
                ? target.VideoProperties.Producers
                : source.VideoProperties.Producers,
            Writers = !string.IsNullOrEmpty(target.VideoProperties.Writers)
                ? target.VideoProperties.Writers
                : source.VideoProperties.Writers,
            Duration = target.VideoProperties.Duration > TimeSpan.Zero
                ? target.VideoProperties.Duration
                : source.VideoProperties.Duration,
            Year = target.VideoProperties.Year > 0
                ? target.VideoProperties.Year
                : source.VideoProperties.Year,
            Width = target.VideoProperties.Width > 0
                ? target.VideoProperties.Width
                : source.VideoProperties.Width,
            Height = target.VideoProperties.Height > 0
                ? target.VideoProperties.Height
                : source.VideoProperties.Height,
            Bitrate = target.VideoProperties.Bitrate > 0
                ? target.VideoProperties.Bitrate
                : source.VideoProperties.Bitrate,
            ShowName = !string.IsNullOrEmpty(target.VideoProperties.ShowName)
                ? target.VideoProperties.ShowName
                : source.VideoProperties.ShowName,
            Season = !string.IsNullOrEmpty(target.VideoProperties.Season)
                ? target.VideoProperties.Season
                : source.VideoProperties.Season,
            Episode = !string.IsNullOrEmpty(target.VideoProperties.Episode)
                ? target.VideoProperties.Episode
                : source.VideoProperties.Episode
        };

        return new MediaInfo(mediaType, musicInfo, videoInfo)
        {
            Size = size,
            DateModified = dateModified,
            ArtworkUri = artworkUri
        };
    }
}

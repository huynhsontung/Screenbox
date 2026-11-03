using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;

namespace Screenbox.Core.Helpers;

internal static class VlcMediaExtensions
{
    public static async Task ParseAsync(this Media media, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // Check if media is already parsed
        if (media.CheckParsed() && media.ParsedStatus is MediaParsedStatus.Done or MediaParsedStatus.Failed)
            return;

        await media.Parse(MediaParseOptions.ParseNetwork, (int)timeout.TotalMilliseconds, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // Media may not be parsed even after calling Parse()
        // This can happen if the media is being open at the same time.
        if (media.CheckParsed() && media.ParsedStatus != MediaParsedStatus.Skipped)
            return;

        // Wait for the ParsedStatus to change again.
        TaskCompletionSource<MediaParsedStatus> tsc = new();
        Task task = tsc.Task;

        media.ParsedChanged += MediaOnParsedChanged;
        try
        {
            if (await Task.WhenAny(tsc.Task, Task.Delay(timeout, cancellationToken)) != task && !task.IsCompleted)
            {
                tsc.SetCanceled();
            }
        }
        finally
        {
            media.ParsedChanged -= MediaOnParsedChanged;
        }

        return;

        void MediaOnParsedChanged(object? sender, MediaParsedChangedEventArgs e)
        {
            tsc.TrySetResult(e.ParsedStatus);
        }
    }

    public static async Task WaitForParsed(this Media media, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (media.CheckParsed()) return;
        TaskCompletionSource<bool> tcs = new();
        Task task = tcs.Task;

        media.ParsedChanged += OnMediaOnParsedChanged;
        try
        {
            if (await Task.WhenAny(tcs.Task, Task.Delay(timeout, cancellationToken)) != task && !task.IsCompleted)
            {
                tcs.SetCanceled();
            }
        }
        finally
        {
            media.ParsedChanged -= OnMediaOnParsedChanged;
        }

        return;

        void OnMediaOnParsedChanged(object? sender, MediaParsedChangedEventArgs args)
        {
            tcs.TrySetResult(args.ParsedStatus == MediaParsedStatus.Done);
        }
    }

    public static bool CheckParsed(this Media media) =>
        media.IsParsed || media.ParsedStatus != 0 || media.State == VLCState.Playing;

    /// <summary>
    /// Extracts a <see cref="MediaInfo"/> snapshot from a LibVLC <see cref="Media"/> instance.
    /// </summary>
    /// <param name="media">The LibVLC media instance to read metadata and track information from.</param>
    /// <returns>A <see cref="MediaInfo"/> populated with the parsed metadata and track properties.</returns>
    public static MediaInfo GetMediaInfo(this Media media)
    {
        MediaPlaybackType mediaType = MediaPlaybackType.Unknown;
        uint videoWidth = 0;
        uint videoHeight = 0;
        uint videoBitrate = 0;
        uint audioBitrate = 0;
        bool hasVideoTrack = false;

        foreach (var track in media.Tracks)
        {
            switch (track.TrackType)
            {
                case TrackType.Video:
                    hasVideoTrack = true;
                    if (videoWidth == 0 && track.Data.Video.Width > 0)
                    {
                        videoWidth = track.Data.Video.Width;
                        videoHeight = track.Data.Video.Height;
                    }

                    if (videoBitrate == 0 && track.Bitrate > 0)
                    {
                        videoBitrate = track.Bitrate;
                    }

                    break;

                case TrackType.Audio:
                    if (audioBitrate == 0 && track.Bitrate > 0)
                    {
                        audioBitrate = track.Bitrate;
                    }

                    break;
            }
        }

        if (hasVideoTrack)
        {
            mediaType = MediaPlaybackType.Video;
        }
        else if (media.ParsedStatus == MediaParsedStatus.Done || media.State == VLCState.Playing || media.Tracks.Length > 0)
        {
            mediaType = MediaPlaybackType.Music;
        }

        TimeSpan duration = media.Duration > 0
            ? TimeSpan.FromMilliseconds(media.Duration)
            : TimeSpan.Zero;
        string title = NormalizeTitle(media.Meta(MetadataType.Title), media.Mrl);
        string artist = media.Meta(MetadataType.Artist) ?? string.Empty;
        string album = media.Meta(MetadataType.Album) ?? string.Empty;
        string albumArtist = media.Meta(MetadataType.AlbumArtist) ?? string.Empty;
        string genre = media.Meta(MetadataType.Genre) ?? string.Empty;
        uint trackNumber = ParseTrackNumber(media.Meta(MetadataType.TrackNumber));
        uint year = ParseYear(media.Meta(MetadataType.Date));
        string description = media.Meta(MetadataType.Description) ?? string.Empty;
        string showName = media.Meta(MetadataType.ShowName) ?? string.Empty;
        string season = media.Meta(MetadataType.Season) ?? string.Empty;
        string episode = media.Meta(MetadataType.Episode) ?? string.Empty;

        Uri? artworkUri = null;
        if (media.Meta(MetadataType.ArtworkURL) is { } artworkUrl &&
            Uri.TryCreate(artworkUrl, UriKind.Absolute, out Uri? parsedArtworkUri) &&
            !parsedArtworkUri.IsFile)
        {
            artworkUri = parsedArtworkUri;
        }

        MusicInfo musicInfo = new()
        {
            Title = title,
            Artist = artist,
            Album = album,
            AlbumArtist = albumArtist,
            Genre = genre,
            TrackNumber = trackNumber,
            Year = year,
            Duration = duration,
            Bitrate = audioBitrate
        };

        VideoInfo videoInfo = new()
        {
            Title = title,
            Subtitle = description,
            Year = year,
            Duration = duration,
            Width = videoWidth,
            Height = videoHeight,
            Bitrate = videoBitrate,
            ShowName = showName,
            Season = season,
            Episode = episode
        };

        return new MediaInfo(mediaType, musicInfo, videoInfo)
        {
            ArtworkUri = artworkUri
        };
    }

    private static string NormalizeTitle(string? rawTitle, string? mrl)
    {
        if (string.IsNullOrWhiteSpace(rawTitle) || Guid.TryParse(rawTitle, out _))
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(mrl) &&
            Uri.TryCreate(mrl, UriKind.Absolute, out Uri? uri) &&
            uri.Segments.Length > 0)
        {
            string defaultSegmentName = Uri.UnescapeDataString(uri.Segments[^1]);
            if (string.Equals(rawTitle, defaultSegmentName, StringComparison.Ordinal))
            {
                return string.Empty;
            }
        }

        return rawTitle;
    }

    private static uint ParseTrackNumber(string? rawTrackNumber)
    {
        if (string.IsNullOrWhiteSpace(rawTrackNumber))
        {
            return 0;
        }

        ReadOnlySpan<char> span = rawTrackNumber.AsSpan().Trim();
        int slashIndex = span.IndexOf('/');
        if (slashIndex >= 0)
        {
            span = span[..slashIndex].Trim();
        }

        return uint.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint trackNumber)
            ? trackNumber
            : 0;
    }

    private static uint ParseYear(string? rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
        {
            return 0;
        }

        ReadOnlySpan<char> span = rawDate.AsSpan().Trim();
        if (uint.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint year))
        {
            return year;
        }

        if (span.Length >= 4 &&
            uint.TryParse(span[..4], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint prefixYear))
        {
            return prefixYear;
        }

        if (DateTimeOffset.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dto))
        {
            return (uint)dto.Year;
        }

        return 0;
    }
}

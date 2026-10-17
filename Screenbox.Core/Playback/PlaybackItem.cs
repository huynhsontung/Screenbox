using System;
using LibVLCSharp.Shared;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Windows.Foundation;

namespace Screenbox.Core.Playback;

public class PlaybackItem
{
    /// <summary>
    /// Occurs when media metadata and track information have been parsed or loaded.
    /// </summary>
    public event TypedEventHandler<PlaybackItem, EventArgs>? MediaParsed;

    internal Media Media { get; }

    public object OriginalSource { get; }

    public bool IsDisabledInPlaybackList { get; set; }

    public PlaybackAudioTrackList AudioTracks { get; }

    public PlaybackVideoTrackList VideoTracks { get; }

    public PlaybackSubtitleTrackList SubtitleTracks { get; }

    public PlaybackChapterList Chapters { get; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan? Duration => Media.Duration > 0 ? TimeSpan.FromMilliseconds(Media.Duration) : null;

    /// <summary>
    /// Gets a value indicating whether the underlying media has finished parsing metadata and tracks.
    /// </summary>
    public bool IsParsed { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="PlaybackItem"/> can be cleaned up and recreated from <see cref="OriginalSource"/>.
    /// </summary>
    public bool CanClean => OriginalSource is not LibVLCSharp.Shared.Media;

    /// <summary>
    /// Gets the parsed artwork URI for the media, if available.
    /// </summary>
    public Uri? ArtworkUri =>
        Media?.Meta(MetadataType.ArtworkURL) is { } artworkUrl &&
        Uri.TryCreate(artworkUrl, UriKind.Absolute, out Uri? artworkUri)
            ? artworkUri
            : null;

    private readonly MediaInfo? _mediaInfoOverride;

    internal PlaybackItem(object source, Media media)
    {
        OriginalSource = source;
        Media = media;
        AudioTracks = new PlaybackAudioTrackList(media);
        VideoTracks = new PlaybackVideoTrackList(media);
        SubtitleTracks = new PlaybackSubtitleTrackList(media);
        Chapters = new PlaybackChapterList(this);
        StartTime = TimeSpan.Zero;
        IsParsed = media.IsParsed && media.ParsedStatus == MediaParsedStatus.Done;
        if (!IsParsed)
        {
            Media.ParsedChanged += Media_ParsedChanged;
        }
    }

    internal PlaybackItem(object source, MediaInfo mediaInfo, bool isParsed = false)
    {
        OriginalSource = source;
        Media = null!;
        AudioTracks = null!;
        VideoTracks = null!;
        SubtitleTracks = null!;
        Chapters = new PlaybackChapterList(this);
        StartTime = TimeSpan.Zero;
        _mediaInfoOverride = mediaInfo;
        IsParsed = isParsed;
    }

    /// <summary>
    /// Extracts a <see cref="MediaInfo"/> snapshot from the underlying media.
    /// </summary>
    /// <returns>A <see cref="MediaInfo"/> containing parsed metadata and track properties.</returns>
    public MediaInfo GetMediaInfo()
    {
        return _mediaInfoOverride ?? Media.GetMediaInfo();
    }

    internal void NotifyTrackInfoLoaded()
    {
        IsParsed = true;
        if (Media is not null)
        {
            Media.ParsedChanged -= Media_ParsedChanged;
        }

        MediaParsed?.Invoke(this, EventArgs.Empty);
    }

    internal void Clean()
    {
        if (Media is not null)
        {
            Media.ParsedChanged -= Media_ParsedChanged;
        }
    }

    private void Media_ParsedChanged(object? sender, MediaParsedChangedEventArgs e)
    {
        if (e.ParsedStatus != MediaParsedStatus.Done)
        {
            return;
        }

        NotifyTrackInfoLoaded();
    }
}

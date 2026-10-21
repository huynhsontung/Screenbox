using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Windows.Media.Core;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;

namespace Screenbox.Core.Playback;

public class PlaybackItem : IPlaybackItem
{
    internal Media Media { get; }

    public IMediaSource Source { get; }

    public object OriginalSource => Source;

    public bool IsDisabledInPlaybackList { get; set; }

    public PlaybackAudioTrackList AudioTracks { get; }

    public PlaybackVideoTrackList VideoTracks { get; }

    public PlaybackSubtitleTrackList SubtitleTracks { get; }

    public PlaybackChapterList Chapters { get; }

    ISingleSelectMediaTrackList<AudioTrack> IPlaybackItem.AudioTracks => AudioTracks;

    ISingleSelectMediaTrackList<VideoTrack> IPlaybackItem.VideoTracks => VideoTracks;

    ISubtitleTrackList IPlaybackItem.SubtitleTracks => SubtitleTracks;

    IPlaybackChapterList IPlaybackItem.Chapters => Chapters;

    public TimeSpan StartTime { get; set; }

    public TimeSpan? Duration => Media.Duration > 0 ? TimeSpan.FromMilliseconds(Media.Duration) : null;

    private readonly Action<Media>? _disposeCallback;
    private readonly IMediaSourceFactory? _sourceFactory;
    private bool _isDisposed;

    public PlaybackItem(IMediaSource source, Media media, Action<Media>? disposeCallback = null, IMediaSourceFactory? sourceFactory = null)
    {
        Source = source;
        Media = media;
        _disposeCallback = disposeCallback;
        _sourceFactory = sourceFactory;
        AudioTracks = new PlaybackAudioTrackList(media);
        VideoTracks = new PlaybackVideoTrackList(media);
        SubtitleTracks = new PlaybackSubtitleTrackList(media);
        Chapters = new PlaybackChapterList(this);
        StartTime = TimeSpan.Zero;
    }

    public async Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await Media.WaitForParsed(timeout, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (!Media.CheckParsed())
        {
            await Media.ParseAsync(timeout, cancellationToken);
        }

        if (Media.SubItems.Count == 0 || _sourceFactory == null)
        {
            return Array.Empty<ExpandedMediaItem>();
        }

        var results = new List<ExpandedMediaItem>(Media.SubItems.Count);
        foreach (var subItem in Media.SubItems)
        {
            var source = _sourceFactory.Create(subItem.Mrl);
            string? title = subItem.Meta(MetadataType.Title);
            TimeSpan? duration = subItem.Duration > 0 ? TimeSpan.FromMilliseconds(subItem.Duration) : null;
            results.Add(new ExpandedMediaItem(source, title, duration));
        }

        return results;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_disposeCallback != null)
        {
            _disposeCallback(Media);
        }
        else
        {
            Media.Dispose();
        }
    }
}

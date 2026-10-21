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

public partial class VlcPlaybackItem : IPlaybackItem
{
    internal Media Media { get; }

    public IMediaSource Source { get; }

    public TimeSpan? Duration => Media.Duration > 0 ? TimeSpan.FromMilliseconds(Media.Duration) : null;

    public VlcAudioTrackList AudioTracks { get; }

    public VlcVideoTrackList VideoTracks { get; }

    public VlcSubtitleTrackList SubtitleTracks { get; }

    public VlcChapterList Chapters { get; }

    ISingleSelectMediaTrackList<AudioTrack> IPlaybackItem.AudioTracks => AudioTracks;

    ISingleSelectMediaTrackList<VideoTrack> IPlaybackItem.VideoTracks => VideoTracks;

    ISubtitleTrackList IPlaybackItem.SubtitleTracks => SubtitleTracks;

    IReadOnlyList<ChapterCue> IPlaybackItem.Chapters => Chapters;

    private readonly Action<Media>? _disposeCallback;
    private readonly IMediaSourceFactory? _sourceFactory;
    private bool _isDisposed;

    public VlcPlaybackItem(
        IMediaSource source,
        Media media,
        Action<Media>? disposeCallback = null,
        IMediaSourceFactory? sourceFactory = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Media = media ?? throw new ArgumentNullException(nameof(media));
        _disposeCallback = disposeCallback;
        _sourceFactory = sourceFactory;
        AudioTracks = new VlcAudioTrackList(media);
        VideoTracks = new VlcVideoTrackList(media);
        SubtitleTracks = new VlcSubtitleTrackList(media);
        Chapters = new VlcChapterList(this);
    }

    public async Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await Media.WaitForParsed(timeout, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return Array.Empty<ExpandedMediaItem>();
        }

        if (!Media.CheckParsed())
        {
            await Media.ParseAsync(timeout, cancellationToken);
        }

        if (_isDisposed)
        {
            return Array.Empty<ExpandedMediaItem>();
        }

        using var subItems = Media.SubItems;
        if (subItems == null || subItems.Count == 0 || _sourceFactory == null)
        {
            return Array.Empty<ExpandedMediaItem>();
        }

        var results = new List<ExpandedMediaItem>(subItems.Count);
        foreach (var subItem in subItems)
        {
            using (subItem)
            {
                var source = _sourceFactory.Create(subItem.Mrl);
                string? title = subItem.Meta(MetadataType.Title);
                TimeSpan? duration = subItem.Duration > 0 ? TimeSpan.FromMilliseconds(subItem.Duration) : null;
                results.Add(new ExpandedMediaItem(source, title, duration));
            }
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

        GC.SuppressFinalize(this);
    }
}

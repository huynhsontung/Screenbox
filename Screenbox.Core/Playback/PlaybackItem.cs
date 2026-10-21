using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Windows.Media.Core;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;

namespace Screenbox.Core.Playback;

/// <summary>
/// Provides an engine-agnostic base implementation of <see cref="IPlaybackItem"/>.
/// </summary>
public abstract partial class PlaybackItem : IPlaybackItem
{
    public IMediaSource Source { get; }

    public bool IsDisabledInPlaybackList { get; set; }

    public TimeSpan StartTime { get; set; }

    public abstract TimeSpan? Duration { get; }

    public abstract ISingleSelectMediaTrackList<AudioTrack> AudioTracks { get; }

    public abstract ISingleSelectMediaTrackList<VideoTrack> VideoTracks { get; }

    public abstract ISubtitleTrackList SubtitleTracks { get; }

    public abstract IPlaybackChapterList Chapters { get; }

    protected PlaybackItem(IMediaSource source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        StartTime = TimeSpan.Zero;
    }

    public virtual Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public virtual Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ExpandedMediaItem>>(Array.Empty<ExpandedMediaItem>());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Windows.Media.Core;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;

namespace Screenbox.Core.Playback;

public interface IPlaybackItem : IDisposable
{
    IMediaSource Source { get; }

    TimeSpan? Duration { get; }

    ISingleSelectMediaTrackList<AudioTrack> AudioTracks { get; }

    ISingleSelectMediaTrackList<VideoTrack> VideoTracks { get; }

    ISubtitleTrackList SubtitleTracks { get; }

    IReadOnlyList<ChapterCue> Chapters { get; }

    Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    Task<MediaInfo?> ProbeMediaInfoAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}

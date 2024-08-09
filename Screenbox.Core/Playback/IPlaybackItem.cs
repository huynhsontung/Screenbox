using System;

namespace Screenbox.Core.Playback;
public interface IPlaybackItem
{
    object OriginalSource { get; }
    PlaybackAudioTrackList AudioTracks { get; }
    PlaybackVideoTrackList VideoTracks { get; }
    PlaybackSubtitleTrackList SubtitleTracks { get; }
    PlaybackChapterList Chapters { get; }
    TimeSpan StartTime { get; set; }
}

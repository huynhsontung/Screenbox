using FFmpegInteropX;
using System;
using Windows.Media.Playback;

namespace Screenbox.Core.Playback;
internal class WindowsPlaybackItem : IPlaybackItem
{
    internal FFmpegMediaSource MediaSource { get; }
    internal MediaPlaybackItem SourceItem { get; }
    public object OriginalSource { get; }
    public PlaybackAudioTrackList AudioTracks { get; }
    public PlaybackVideoTrackList VideoTracks { get; }
    public PlaybackSubtitleTrackList SubtitleTracks { get; }
    public PlaybackChapterList Chapters { get; }
    public TimeSpan StartTime { get; set; }

    public WindowsPlaybackItem(FFmpegMediaSource source)
    {
        MediaSource = source;
        SourceItem = source.CreateMediaPlaybackItem();
        OriginalSource = source;
        AudioTracks = new PlaybackAudioTrackList(SourceItem.AudioTracks);
        VideoTracks = new PlaybackVideoTrackList(SourceItem.VideoTracks);
        SubtitleTracks = new PlaybackSubtitleTrackList(SourceItem.TimedMetadataTracks);
        Chapters = new PlaybackChapterList(SourceItem.TimedMetadataTracks, this);
        StartTime = TimeSpan.Zero;
    }
}

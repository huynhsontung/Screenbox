using System;
using Windows.Media.Playback;

namespace Screenbox.Core.Playback;
internal class WindowsPlaybackItem : IPlaybackItem
{
    internal MediaPlaybackItem MediaSource { get; }
    public object OriginalSource { get; }
    public PlaybackAudioTrackList AudioTracks { get; }
    public PlaybackVideoTrackList VideoTracks { get; }
    public PlaybackSubtitleTrackList SubtitleTracks { get; }
    public PlaybackChapterList Chapters { get; }
    public TimeSpan StartTime { get; set; }

    public WindowsPlaybackItem(MediaPlaybackItem source)
    {
        MediaSource = source;
        OriginalSource = source;
        AudioTracks = new PlaybackAudioTrackList(source.AudioTracks);
        VideoTracks = new PlaybackVideoTrackList(source.VideoTracks);
        SubtitleTracks = new PlaybackSubtitleTrackList(source.TimedMetadataTracks);
        Chapters = new PlaybackChapterList(source.TimedMetadataTracks, this);
        StartTime = TimeSpan.Zero;
    }
}

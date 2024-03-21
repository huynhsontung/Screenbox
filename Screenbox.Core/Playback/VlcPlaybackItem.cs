#nullable enable

using LibVLCSharp.Shared;
using System;

namespace Screenbox.Core.Playback
{
    public class VlcPlaybackItem : IPlaybackItem
    {
        internal Media Media { get; }

        public object OriginalSource { get; }

        public bool IsDisabledInPlaybackList { get; set; }

        public PlaybackAudioTrackList AudioTracks { get; }

        public PlaybackVideoTrackList VideoTracks { get; }

        public PlaybackSubtitleTrackList SubtitleTracks { get; }

        public PlaybackChapterList Chapters { get; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan? Duration => Media.Duration > 0 ? TimeSpan.FromMilliseconds(Media.Duration) : null;

        internal VlcPlaybackItem(object source, Media media)
        {
            OriginalSource = source;
            Media = media;
            AudioTracks = new PlaybackAudioTrackList(media);
            VideoTracks = new PlaybackVideoTrackList(media);
            SubtitleTracks = new PlaybackSubtitleTrackList(media);
            Chapters = new PlaybackChapterList(this);
            StartTime = TimeSpan.Zero;
        }
    }
}

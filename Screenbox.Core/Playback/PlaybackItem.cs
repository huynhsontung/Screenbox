#nullable enable

using System;
using LibVLCSharp.Shared;
using Screenbox.Core.Services;

namespace Screenbox.Core.Playback
{
    public class PlaybackItem
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

        public PlaybackItem(object source, IMediaService mediaService)
        {
            OriginalSource = source;
            Media media = mediaService.CreateMedia(source);
            Media = media;
            AudioTracks = new PlaybackAudioTrackList(media);
            VideoTracks = new PlaybackVideoTrackList(media);
            SubtitleTracks = new PlaybackSubtitleTrackList(media);
            Chapters = new PlaybackChapterList();
            StartTime = TimeSpan.Zero;
        }
    }
}

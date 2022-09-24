#nullable enable

using System;
using LibVLCSharp.Shared;
using Windows.Storage;
using Screenbox.Services;

namespace Screenbox.Core.Playback
{
    public class PlaybackItem
    {
        public Media Source { get; }

        public bool IsDisabledInPlaybackList { get; set; }

        public PlaybackAudioTrackList AudioTracks { get; }

        public PlaybackVideoTrackList VideoTracks { get; }

        public PlaybackSubtitleTrackList SubtitleTracks { get; }

        public PlaybackChapterList Chapters { get; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan? Duration => Source.Duration > 0 ? TimeSpan.FromMilliseconds(Source.Duration) : null;

        private PlaybackItem(Media media)
        {
            Source = media;
            AudioTracks = new PlaybackAudioTrackList(media);
            VideoTracks = new PlaybackVideoTrackList(media);
            SubtitleTracks = new PlaybackSubtitleTrackList(media);
            Chapters = new PlaybackChapterList();
            StartTime = TimeSpan.Zero;
        }

        internal PlaybackItem(IMediaService mediaService, IStorageFile file) : this(mediaService.CreateMedia(file))
        {
        }

        internal PlaybackItem(IMediaService mediaService, Uri uri) : this(mediaService.CreateMedia(uri))
        {
        }
    }
}

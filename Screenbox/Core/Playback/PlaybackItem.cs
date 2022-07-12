#nullable enable

using System;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using Microsoft.Toolkit.Diagnostics;

namespace Screenbox.Core.Playback
{
    public class PlaybackItem
    {
        public Media Source { get; }

        public bool IsDisabledInPlaybackList { get; set; }

        public PlaybackAudioTrackList AudioTracks { get; }

        public PlaybackSubtitleTrackList SubtitleTracks { get; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan? Duration => Source.IsParsed ? TimeSpan.FromMilliseconds(Source.Duration) : null;

        private static readonly Dictionary<string, PlaybackItem> Items = new();

        private PlaybackItem(Media media)
        {
            Source = media;
            Items[media.Mrl] = this;
            AudioTracks = new PlaybackAudioTrackList(media);
            SubtitleTracks = new PlaybackSubtitleTrackList(media);
            StartTime = TimeSpan.Zero;
        }

        public static PlaybackItem GetFromVlcMedia(Media media)
        {
            Guard.IsNotNullOrEmpty(media.Mrl, nameof(media.Mrl));
            return Items.TryGetValue(media.Mrl, out PlaybackItem? item) ? item : new PlaybackItem(media);
        }

        // TODO: Implement clean up queue
    }
}

#nullable enable

using System;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using Microsoft.Toolkit.Diagnostics;

namespace Screenbox.Core.Playback
{
    internal class PlaybackItem
    {
        public Media Source { get; }

        public bool IsDisabledInPlaybackList { get; set; }

        public PlaybackAudioTrackList AudioTracks { get; }

        public PlaybackSubtitleTrackList SubtitleTracks { get; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan Duration => TimeSpan.FromMilliseconds(Source.Duration);

        private static readonly Dictionary<string, PlaybackItem> Items = new();

        private PlaybackItem(Media media)
        {
            Source = media;
            Items[media.Mrl] = this;
            AudioTracks = new PlaybackAudioTrackList(media);
            SubtitleTracks = new PlaybackSubtitleTrackList(media);
        }

        public static PlaybackItem GetFromVlcMedia(Media media)
        {
            Guard.IsNotNullOrEmpty(media.Mrl, nameof(media.Mrl));
            return Items.TryGetValue(media.Mrl, out PlaybackItem? item) ? item : new PlaybackItem(media);
        }
    }
}

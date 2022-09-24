#nullable enable

using System;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
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

        private static readonly Dictionary<string, PlaybackItem> Items = new();

        private PlaybackItem(Media media)
        {
            Source = media;
            AudioTracks = new PlaybackAudioTrackList(media);
            VideoTracks = new PlaybackVideoTrackList(media);
            SubtitleTracks = new PlaybackSubtitleTrackList(media);
            Chapters = new PlaybackChapterList();
            StartTime = TimeSpan.Zero;
        }

        public static PlaybackItem GetSingleton(StorageFile file)
        {
            string path = file.Path;
            if (!string.IsNullOrEmpty(path) && Items.TryGetValue(path, out PlaybackItem? item))
            {
                return item;
            }

            IMediaService mediaService = App.Services.GetRequiredService<IMediaService>();
            Media media = mediaService.CreateMedia(file);
            item = new PlaybackItem(media);
            if (!string.IsNullOrEmpty(path)) Items.Add(path, item);
            return item;
        }

        public static PlaybackItem GetSingleton(Uri uri)
        {
            string uriString = uri.ToString();
            if (Items.TryGetValue(uriString, out PlaybackItem? item))
            {
                return item;
            }

            IMediaService mediaService = App.Services.GetRequiredService<IMediaService>();
            Media media = mediaService.CreateMedia(uri);
            item = new PlaybackItem(media);
            Items.Add(uriString, item);
            return item;
        }

        // TODO: Implement clean up queue
    }
}

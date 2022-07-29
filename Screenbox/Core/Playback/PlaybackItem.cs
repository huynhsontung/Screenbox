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

        public PlaybackSubtitleTrackList SubtitleTracks { get; }

        public PlaybackChapterList Chapters { get; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan? Duration => Source.Duration > 0 ? TimeSpan.FromMilliseconds(Source.Duration) : null;

        private static readonly Dictionary<Uri, PlaybackItem> ItemsWithUri = new();
        private static readonly Dictionary<StorageFile, PlaybackItem> ItemsWithStorage = new();

        private PlaybackItem(Media media)
        {
            Source = media;
            AudioTracks = new PlaybackAudioTrackList(media);
            SubtitleTracks = new PlaybackSubtitleTrackList(media);
            Chapters = new PlaybackChapterList();
            StartTime = TimeSpan.Zero;
        }

        public static PlaybackItem GetFromStorageFile(StorageFile file)
        {
            if (ItemsWithStorage.TryGetValue(file, out PlaybackItem? item))
            {
                return item;
            }

            IMediaService mediaService = App.Services.GetRequiredService<IMediaService>();
            Media media = mediaService.CreateMedia(file);
            item = new PlaybackItem(media);
            ItemsWithStorage.Add(file, item);
            return item;
        }

        public static PlaybackItem GetFromUri(Uri uri)
        {
            if (ItemsWithUri.TryGetValue(uri, out PlaybackItem? item))
            {
                return item;
            }

            IMediaService mediaService = App.Services.GetRequiredService<IMediaService>();
            Media media = mediaService.CreateMedia(uri);
            item = new PlaybackItem(media);
            ItemsWithUri.Add(uri, item);
            return item;
        }

        // TODO: Implement clean up queue
    }
}

﻿#nullable enable

using Windows.Media.Core;
using LibVLCSharp.Shared;
using CommunityToolkit.Diagnostics;

namespace Screenbox.Core.Playback
{
    public sealed class VideoTrack : IMediaTrack
    {
        public string Id { get; }

        public string? Label { get; set; }

        public string? Language { get; }

        public MediaTrackKind TrackKind => MediaTrackKind.Video;

        public VideoTrack(MediaTrack videoTrack)
        {
            Guard.IsTrue(videoTrack.TrackType == TrackType.Video, nameof(videoTrack.TrackType));
            Id = videoTrack.Id.ToString();
            Language = videoTrack.Language;
            Label = videoTrack.Description ?? videoTrack.Language;
        }
    }
}

#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public sealed class VideoTrack : IMediaTrack
    {
        public string Id { get; }

        public string Label { get; set; }

        public string Language { get; }

        public MediaTrackKind TrackKind => MediaTrackKind.Video;

        public VideoTrack(MediaTrack videoTrack)
        {
            Guard.IsTrue(videoTrack.TrackType == TrackType.Video, nameof(videoTrack.TrackType));
            Id = videoTrack.Id.ToString();
            Language = videoTrack.Language ?? string.Empty;
            Label = videoTrack.Description ?? videoTrack.Language ?? string.Empty;
        }

        public VideoTrack(Windows.Media.Core.VideoTrack videoTrack)
        {
            Id = videoTrack.Id;
            Label = videoTrack.Label;
            Language = videoTrack.Language;
        }
    }
}

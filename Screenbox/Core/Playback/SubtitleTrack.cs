#nullable enable

using Windows.Media.Core;
using LibVLCSharp.Shared;
using CommunityToolkit.Diagnostics;

namespace Screenbox.Core.Playback
{
    public sealed class SubtitleTrack : IMediaTrack
    {
        internal int VlcSpu { get; set; }

        public string Id { get; }

        public string? Label { get; set; }

        public string? Language { get; }

        public MediaTrackKind TrackKind => MediaTrackKind.TimedMetadata;

        public SubtitleTrack(MediaTrack textTrack)
        {
            Guard.IsTrue(textTrack.TrackType == TrackType.Text, nameof(textTrack.TrackType));
            VlcSpu = textTrack.Id;
            Id = textTrack.Id.ToString();
            Language = textTrack.Language;
            Label = string.IsNullOrEmpty(textTrack.Description)
                ? textTrack.Language
                : $"{textTrack.Description} ({textTrack.Language})";
        }
    }
}

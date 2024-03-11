#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public sealed class SubtitleTrack : IMediaTrack
    {
        internal int VlcSpu { get; set; }

        public string Id { get; }

        public string Label { get; set; }

        public string Language { get; }

        public MediaTrackKind TrackKind => MediaTrackKind.TimedMetadata;

        public SubtitleTrack(MediaTrack textTrack)
        {
            Guard.IsTrue(textTrack.TrackType == TrackType.Text, nameof(textTrack.TrackType));
            VlcSpu = textTrack.Id;
            Id = textTrack.Id.ToString();
            Language = textTrack.Language ?? string.Empty;
            Label = string.IsNullOrEmpty(textTrack.Description)
                ? textTrack.Language ?? string.Empty
                : $"{textTrack.Description} ({textTrack.Language})";
        }

        public SubtitleTrack(TimedMetadataTrack track)
        {
            Id = track.Id;
            Label = track.Label;
            Language = track.Language;
        }
    }
}

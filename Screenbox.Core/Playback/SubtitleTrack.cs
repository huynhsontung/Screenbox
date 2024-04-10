#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public sealed class SubtitleTrack : MediaTrack
    {
        internal int VlcSpu { get; set; }

        public SubtitleTrack(string language = "") : base(MediaTrackKind.TimedMetadata, language)
        {
        }

        public SubtitleTrack(LibVLCSharp.Shared.MediaTrack textTrack) : base(textTrack)
        {
            Guard.IsTrue(textTrack.TrackType == TrackType.Text, nameof(textTrack.TrackType));
            VlcSpu = textTrack.Id;
        }

        public SubtitleTrack(TimedMetadataTrack track) : base(track)
        {
        }
    }
}

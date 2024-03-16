#nullable enable

using Windows.Media.Core;
using LibVLCSharp.Shared;
using CommunityToolkit.Diagnostics;

namespace Screenbox.Core.Playback
{
    public sealed class SubtitleTrack : MediaTrack
    {
        internal int VlcSpu { get; }

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

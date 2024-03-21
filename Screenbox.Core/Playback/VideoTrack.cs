#nullable enable

namespace Screenbox.Core.Playback
{
    public sealed class VideoTrack : MediaTrack
    {
        public VideoTrack(LibVLCSharp.Shared.MediaTrack videoTrack) : base(videoTrack)
        {
        }

        public VideoTrack(Windows.Media.Core.VideoTrack videoTrack) : base(videoTrack)
        {
        }
    }
}

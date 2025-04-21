#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback;

public sealed class VideoTrack : MediaTrack
{
    internal int VlcTrackId { get; }

    public string Name { get; }

    public VideoTrack(LibVLCSharp.Shared.MediaTrack videoTrack) : base(videoTrack)
    {
        Guard.IsTrue(videoTrack.TrackType == TrackType.Video, nameof(videoTrack.TrackType));
        VlcTrackId = videoTrack.Id;
        Name = videoTrack.Description ?? videoTrack.Language ?? videoTrack.Id.ToString();
    }

    public VideoTrack(Windows.Media.Core.VideoTrack videoTrack) : base(videoTrack)
    {
        Name = videoTrack.Name;
    }
}

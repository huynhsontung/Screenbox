#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback;
public sealed class AudioTrack : MediaTrack
{
    internal int VlcTrackId { get; }

    public string Name { get; }

    public AudioTrack(LibVLCSharp.Shared.MediaTrack audioTrack) : base(audioTrack)
    {
        Guard.IsTrue(audioTrack.TrackType == TrackType.Audio, nameof(audioTrack.TrackType));
        VlcTrackId = audioTrack.Id;
        Name = audioTrack.Description ?? audioTrack.Language ?? audioTrack.Id.ToString();
    }

    public AudioTrack(Windows.Media.Core.AudioTrack audioTrack) : base(audioTrack)
    {
        Name = audioTrack.Name;
    }
}
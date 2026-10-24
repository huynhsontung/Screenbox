using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public sealed partial class VideoTrack : MediaTrack
{
    public string Name { get; }

    public VideoTrack(string id, string name, string? label = null, string language = "")
        : base(MediaTrackKind.Video, id, label ?? name, language)
    {
        Name = name;
    }

    public VideoTrack(Windows.Media.Core.VideoTrack videoTrack) : base(videoTrack)
    {
        Name = videoTrack.Name;
    }
}

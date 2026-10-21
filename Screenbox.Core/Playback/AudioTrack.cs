using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public sealed partial class AudioTrack : MediaTrack
{
    public string Name { get; }

    public AudioTrack(string id, string name, string? label = null, string language = "")
        : base(MediaTrackKind.Audio, id, label ?? name, language)
    {
        Name = name;
    }

    public AudioTrack(Windows.Media.Core.AudioTrack audioTrack) : base(audioTrack)
    {
        Name = audioTrack.Name;
    }
}

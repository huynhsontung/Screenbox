using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public sealed partial class SubtitleTrack : MediaTrack
{
    public SubtitleTrack(string id = "", string? label = "", string language = "")
        : base(MediaTrackKind.TimedMetadata, id, label ?? string.Empty, language)
    {
    }

    public SubtitleTrack(TimedMetadataTrack track) : base(track)
    {
    }
}

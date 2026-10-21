using Windows.Storage;

namespace Screenbox.Core.Playback;

public interface ISubtitleTrackList : ISingleSelectMediaTrackList<SubtitleTrack>
{
    void AddExternalSubtitle(IStorageFile file, bool select = true);
}

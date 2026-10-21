using System.Collections.Generic;
using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public interface ISingleSelectMediaTrackList<out T> : IReadOnlyList<T>, ISingleSelectMediaTrackList where T : IMediaTrack
{
    void Refresh();
}

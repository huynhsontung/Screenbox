using System.Collections.Generic;
using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public interface IPlaybackChapterList : IReadOnlyList<ChapterCue>
{
    void Load(IMediaPlayer player);
}

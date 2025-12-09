#nullable enable

using Screenbox.Core.Playback;

namespace Screenbox.Core.Contexts;

internal sealed class VolumeContext
{
    internal int MaxVolume { get; set; }
    internal int Volume { get; set; }
    internal bool IsMute { get; set; }
    internal IMediaPlayer? MediaPlayer { get; set; }
    internal bool IsInitialized { get; set; }
}

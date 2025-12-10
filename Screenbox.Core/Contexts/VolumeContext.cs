#nullable enable

using Screenbox.Core.Playback;

namespace Screenbox.Core.Contexts;

public sealed class VolumeContext
{
    public int MaxVolume { get; set; }
    public int Volume { get; set; }
    public bool IsMute { get; set; }
    public IMediaPlayer? MediaPlayer { get; set; }
    public bool IsInitialized { get; set; }
}

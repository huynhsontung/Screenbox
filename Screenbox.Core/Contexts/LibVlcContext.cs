#nullable enable

using LibVLCSharp.Shared;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Contexts;

public sealed class LibVlcContext
{
    public VlcMediaPlayer? MediaPlayer { get; set; }
    public LibVLC? LibVlc { get; set; }
    public bool UseFutureAccessList { get; set; } = true;
}

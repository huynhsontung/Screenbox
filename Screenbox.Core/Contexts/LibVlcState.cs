#nullable enable

using LibVLCSharp.Shared;

namespace Screenbox.Core.Contexts;

internal sealed class LibVlcState
{
    internal VlcMediaPlayer? MediaPlayer { get; set; }
    internal LibVLC? LibVlc { get; set; }
    internal bool UseFutureAccessList { get; set; } = true;
}

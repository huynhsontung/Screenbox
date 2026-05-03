#nullable enable

namespace Screenbox.Casting.Models;

public enum CastSessionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    ReceiverLaunching = 3,
    ReceiverReady = 4,
    MediaLoaded = 5,
    Playing = 6,
    Paused = 7,
    Stopped = 8,
    Faulted = 9,
}

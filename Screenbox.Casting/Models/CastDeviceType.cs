#nullable enable

namespace Screenbox.Casting.Models;

/// <summary>
/// Identifies the network casting protocol a device uses.
/// </summary>
public enum CastDeviceType
{
    /// <summary>Google Cast (Chromecast) protocol via SharpCaster.</summary>
    Chromecast,

    /// <summary>DLNA/UPnP Digital Media Renderer (DMR) protocol.</summary>
    Dlna,
}

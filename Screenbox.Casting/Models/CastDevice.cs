#nullable enable

namespace Screenbox.Casting.Models;

/// <summary>
/// Represents a discovered cast-capable target device.
/// </summary>
public sealed class CastDevice
{
    /// <summary>
    /// Initializes a new <see cref="CastDevice"/> instance.
    /// </summary>
    public CastDevice(string id, string name, string host, int port, CastProtocol protocol, bool canRenderVideo, bool canRenderAudio, string? model = null, string? iconUri = null)
    {
        Id = id;
        Name = name;
        Host = host;
        Port = port;
        Protocol = protocol;
        CanRenderVideo = canRenderVideo;
        CanRenderAudio = canRenderAudio;
        Model = model;
        IconUri = iconUri;
    }

    public string Id { get; }

    public string Name { get; }

    public string Host { get; }

    public int Port { get; }

    public CastProtocol Protocol { get; }

    public string? Model { get; }

    public string? IconUri { get; }

    public bool CanRenderVideo { get; }

    public bool CanRenderAudio { get; }
}

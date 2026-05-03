#nullable enable

using System;
using Windows.Networking.Sockets;
using Windows.Storage;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the runtime state for the local HTTP streaming server used to serve media files to Chromecast devices.
/// </summary>
public sealed class MediaStreamingContext : IDisposable
{
    /// <summary>The active <see cref="StreamSocketListener"/> bound to a local port, or <c>null</c> when the server is not running.</summary>
    public StreamSocketListener? Listener { get; set; }

    /// <summary>The file currently being served over HTTP, or <c>null</c> when no file is active.</summary>
    public IStorageFile? CurrentFile { get; set; }

    /// <summary>Stops the active HTTP server and releases all held resources.</summary>
    public void Dispose()
    {
        Listener?.Dispose();
        Listener = null;
        CurrentFile = null;
    }
}

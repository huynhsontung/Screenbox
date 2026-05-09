#nullable enable

using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Casting.Abstractions;

/// <summary>
/// Proxies a local <see cref="IStorageFile"/> over HTTP so that cast devices on the local
/// network can fetch it by URL.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="StartAsync"/> binds a new TCP port and returns an
/// <see cref="ICastProxyHandle"/> whose <see cref="ICastProxyHandle.Url"/> is the address
/// the cast device should load.  Disposing the handle stops the server and releases the port.
/// </para>
/// <para>
/// This interface is intentionally narrow: it only proxies local files.  Callers are
/// responsible for deciding whether a proxy is needed (e.g., skip the proxy for
/// http/https sources the cast device can already reach).
/// </para>
/// </remarks>
public interface ICastMediaProxy
{
    /// <summary>
    /// Starts an HTTP proxy for <paramref name="file"/> and returns a handle to the active
    /// proxy session.
    /// </summary>
    /// <param name="file">The local file to serve.</param>
    /// <returns>
    /// A handle whose <see cref="ICastProxyHandle.Url"/> can be passed to a cast device.
    /// Dispose the handle when the cast session ends.
    /// </returns>
    Task<ICastProxyHandle> StartAsync(IStorageFile file);
}

#nullable enable

using System;

namespace Screenbox.Casting.Abstractions;

/// <summary>
/// A handle representing an active local HTTP proxy session for a media file.
/// Dispose the handle to stop serving the file and release the bound port.
/// </summary>
public interface ICastProxyHandle : IDisposable
{
    /// <summary>Gets the URL a cast device should use to fetch the proxied file.</summary>
    Uri Url { get; }
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Chromecast;
using Screenbox.Casting.Dlna;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="ICastService"/> by bridging <see cref="ChromecastDeviceLocator"/>
/// and <see cref="DlnaDeviceLocator"/> with the local HTTP proxy infrastructure.
///
/// <para>
/// This service is stateless with respect to the active session — callers own the
/// <see cref="ICastSession"/> and are responsible for its lifecycle.
/// </para>
/// </summary>
public sealed class CastService : ICastService
{
    private readonly ICastMediaProxy _mediaProxy;

    public CastService(ICastMediaProxy mediaProxy)
    {
        _mediaProxy = mediaProxy;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ICastDeviceLocator> CreateLocators()
    {
        return new List<ICastDeviceLocator>
        {
            new ChromecastDeviceLocator(),
            new DlnaDeviceLocator(),
        };
    }

    /// <inheritdoc/>
    public async Task<ICastSession?> ConnectAndCastAsync(ICastDevice device, PlaybackItem item, TimeSpan startPosition)
    {
        ICastProxyHandle? proxyHandle = null;
        try
        {
            Uri? streamUrl;

            if (TryGetNetworkUri(item.OriginalSource, out Uri? networkUri))
            {
                // Cast device can reach this URL directly — no proxy needed.
                streamUrl = networkUri;
            }
            else if (item.OriginalSource is Windows.Storage.IStorageFile file)
            {
                proxyHandle = await _mediaProxy.StartAsync(file);
                streamUrl = proxyHandle.Url;
            }
            else
            {
                return null;
            }

            ICastDeviceLocator locator = device.Type == Casting.Models.CastDeviceType.Dlna
                ? new DlnaDeviceLocator()
                : new ChromecastDeviceLocator();

            ICastSession? session = await locator.ConnectAsync(device, streamUrl, startPosition).ConfigureAwait(false);
            if (session is null)
            {
                proxyHandle?.Dispose();
                return null;
            }

            // Wrap so the proxy handle is automatically released when the session ends.
            return new ProxiedCastSession(session, proxyHandle);
        }
        catch (Exception)
        {
            proxyHandle?.Dispose();
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(ICastSession? session = null)
    {
        if (session is not null)
        {
            try { await session.StopAsync().ConfigureAwait(false); } catch { }
            // Dispose releases both the session resources and the proxy handle
            // (when session is a ProxiedCastSession).
            try { session.Dispose(); } catch { }
        }
    }

    private static bool TryGetNetworkUri(object? source, out Uri? uri)
    {
        uri = source switch
        {
            Uri u when u.Scheme is "http" or "https" => u,
            string s when Uri.TryCreate(s, UriKind.Absolute, out Uri? parsed)
                          && parsed.Scheme is "http" or "https" => parsed,
            _ => null
        };

        return uri is not null;
    }

    // -------------------------------------------------------------------------
    // Wraps ICastSession + ICastProxyHandle so both are disposed together
    // -------------------------------------------------------------------------

    private sealed class ProxiedCastSession : ICastSession
    {
        public ICastDevice Device => _inner.Device;
        public double Position => _inner.Position;
        public double Duration => _inner.Duration;
        public bool IsPlaying => _inner.IsPlaying;
        public bool IsBuffering => _inner.IsBuffering;
        public double Volume => _inner.Volume;
        public bool IsMuted => _inner.IsMuted;

        public event EventHandler? PlaybackEnded;
        public event EventHandler? Disconnected;

        private readonly ICastSession _inner;
        private readonly ICastProxyHandle? _proxyHandle;

        internal ProxiedCastSession(ICastSession inner, ICastProxyHandle? proxyHandle)
        {
            _inner = inner;
            _proxyHandle = proxyHandle;
            _inner.PlaybackEnded += (s, e) => PlaybackEnded?.Invoke(this, e);
            _inner.Disconnected += (s, e) => Disconnected?.Invoke(this, e);
        }

        public Task PlayAsync() => _inner.PlayAsync();
        public Task PauseAsync() => _inner.PauseAsync();
        public Task StopAsync() => _inner.StopAsync();
        public Task SeekAsync(TimeSpan position) => _inner.SeekAsync(position);
        public Task SetVolumeAsync(double level) => _inner.SetVolumeAsync(level);
        public Task SetMuteAsync(bool muted) => _inner.SetMuteAsync(muted);

        public void Dispose()
        {
            _inner.Dispose();
            _proxyHandle?.Dispose();
        }
    }
}


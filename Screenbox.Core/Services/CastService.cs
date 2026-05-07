#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Chromecast;
using Screenbox.Casting.Dlna;
using Screenbox.Core.Playback;
using Windows.Storage;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="ICastService"/> by bridging <see cref="ChromecastDeviceLocator"/>
/// and <see cref="DlnaDeviceLocator"/> with the local HTTP streaming infrastructure.
///
/// <para>
/// This service is stateless with respect to the active session — callers own the
/// <see cref="ICastSession"/> and are responsible for its lifecycle.
/// </para>
/// </summary>
public sealed class CastService : ICastService
{
    private readonly IMediaStreamingService _streamingService;

    public CastService(IMediaStreamingService streamingService)
    {
        _streamingService = streamingService;
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
        try
        {
            // Resolve the media URL (or start local HTTP server for local files).
            Uri? streamUrl = await _streamingService.StartStreamAsync(item);
            if (streamUrl is null)
            {
                return null;
            }

            // Delegate to the protocol-appropriate locator.
            ICastDeviceLocator locator = device.Type == Casting.Models.CastDeviceType.Dlna
                ? new DlnaDeviceLocator()
                : new ChromecastDeviceLocator();

            ICastSession? session = await locator.ConnectAsync(device, streamUrl, startPosition).ConfigureAwait(false);
            return session;
        }
        catch (Exception)
        {
            _streamingService.StopStream();
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(ICastSession? session = null)
    {
        if (session is not null)
        {
            try { await session.StopAsync().ConfigureAwait(false); } catch { }
            try { session.Dispose(); } catch { }
        }

        // Stop the local HTTP server after the session has been cleaned up.
        _streamingService.StopStream();
    }
}


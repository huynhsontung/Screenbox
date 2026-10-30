using System;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Default <see cref="IMediaProbeService"/> implementation backed by LibVLC media parsing.
/// </summary>
public sealed class VlcMediaProbeService : IMediaProbeService
{
    private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(10);

    private readonly PlayerContext _playerContext;
    private readonly IPlayerService _playerService;
    private readonly IPlayQueueBufferManager _bufferManager;
    private readonly ILogger<VlcMediaProbeService> _logger;

    public VlcMediaProbeService(
        PlayerContext playerContext,
        IPlayerService playerService,
        IPlayQueueBufferManager bufferManager,
        ILogger<VlcMediaProbeService> logger)
    {
        _playerContext = playerContext;
        _playerService = playerService;
        _bufferManager = bufferManager;
        _logger = logger;
    }

    /// <summary>
    /// Probes LibVLC media properties from the specified <paramref name="media"/> instance.
    /// </summary>
    /// <param name="media">The LibVLC media to probe.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if probing was canceled or failed.</returns>
    public async Task<MediaInfo?> ProbeMediaAsync(
        Media media,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);

        try
        {
            return await ProbeMediaInternalAsync(media, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to probe media properties.");
            return null;
        }
    }

    /// <summary>
    /// Probes LibVLC media properties from the specified <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The VLC playback item to probe.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if probing was canceled or failed.</returns>
    public Task<MediaInfo?> ProbeMediaAsync(
        VlcPlaybackItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return ProbeMediaAsync(item.Media, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<MediaInfo?> ProbeMediaAsync(
        IPlaybackItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is not VlcPlaybackItem vlcItem)
        {
            throw new NotSupportedException("Only VlcPlaybackItem is supported.");
        }

        return ProbeMediaAsync(vlcItem, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MediaInfo?> ProbeMediaAsync(
        IMediaSource source,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_playerContext.MediaPlayer is { PlaybackItem: VlcPlaybackItem activeItem } &&
            ReferenceEquals(activeItem.Source, source))
        {
            return await ProbeMediaAsync(activeItem, cancellationToken);
        }

        if (_bufferManager.TryGetPlaybackItem(source, out IPlaybackItem? bufferedItem) &&
            bufferedItem is VlcPlaybackItem bufferedVlcItem)
        {
            return await ProbeMediaAsync(bufferedVlcItem, cancellationToken);
        }

        if (_playerContext.MediaPlayer is not { } mediaPlayer)
        {
            return null;
        }

        try
        {
            using var tempItem = _playerService.CreatePlaybackItem(
                mediaPlayer,
                source,
                options ?? Array.Empty<string>());
            return await ProbeMediaAsync(tempItem, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to probe media properties.");
            return null;
        }
    }

    private static async Task<MediaInfo?> ProbeMediaInternalAsync(Media media, CancellationToken cancellationToken)
    {
        if (!media.IsParsed || media.ParsedStatus != MediaParsedStatus.Done)
        {
            await media.ParseAsync(DefaultProbeTimeout, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (media.ParsedStatus != MediaParsedStatus.Done &&
            media.State != VLCState.Playing &&
            media.Tracks.Length == 0)
        {
            return null;
        }

        return media.GetMediaInfo();
    }
}

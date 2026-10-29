using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Default <see cref="IMediaProbeService"/> implementation backed by the playback engine.
/// </summary>
public sealed class MediaProbeService : IMediaProbeService
{
    private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(10);

    private readonly PlayerContext _playerContext;
    private readonly IPlayerService _playerService;
    private readonly IPlayQueueBufferManager _bufferManager;
    private readonly ILogger<MediaProbeService> _logger;

    public MediaProbeService(
        PlayerContext playerContext,
        IPlayerService playerService,
        IPlayQueueBufferManager bufferManager,
        ILogger<MediaProbeService> logger)
    {
        _playerContext = playerContext;
        _playerService = playerService;
        _bufferManager = bufferManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MediaInfo?> ProbeMediaAsync(
        IPlaybackItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            return await item.ProbeMediaInfoAsync(DefaultProbeTimeout, cancellationToken);
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

    /// <inheritdoc/>
    public async Task<MediaInfo?> ProbeMediaAsync(
        IMediaSource source,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_playerContext.MediaPlayer is { PlaybackItem: { } activeItem } &&
            ReferenceEquals(activeItem.Source, source))
        {
            return await ProbeMediaAsync(activeItem, cancellationToken);
        }

        if (_bufferManager.TryGetPlaybackItem(source, out IPlaybackItem? bufferedItem))
        {
            return await ProbeMediaAsync(bufferedItem, cancellationToken);
        }

        if (_playerContext.MediaPlayer is not { } mediaPlayer)
        {
            return null;
        }

        IPlaybackItem? tempItem = null;
        try
        {
            tempItem = _playerService.CreatePlaybackItem(
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
        finally
        {
            tempItem?.Dispose();
        }
    }
}

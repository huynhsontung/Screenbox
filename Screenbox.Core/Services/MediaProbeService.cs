using System;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Default <see cref="IMediaProbeService"/> implementation backed by LibVLC media parsing.
/// </summary>
public sealed class MediaProbeService : IMediaProbeService
{
    private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(10);

    private readonly PlayerContext _playerContext;
    private readonly IPlayerService _playerService;
    private readonly ILogger<MediaProbeService> _logger;

    public MediaProbeService(
        PlayerContext playerContext,
        IPlayerService playerService,
        ILogger<MediaProbeService> logger)
    {
        _playerContext = playerContext;
        _playerService = playerService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MediaInfo?> ProbeMediaAsync(
        object source,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            switch (source)
            {
                case PlaybackItem existingItem:
                    return await ProbeMediaInternalAsync(existingItem.Media, cancellationToken);

                case Media existingMedia:
                    return await ProbeMediaInternalAsync(existingMedia, cancellationToken);

                default:
                    if (_playerContext.MediaPlayer is not { } mediaPlayer)
                    {
                        return null;
                    }

                    PlaybackItem? tempItem = null;
                    try
                    {
                        tempItem = _playerService.CreatePlaybackItem(
                            mediaPlayer,
                            source,
                            options ?? Array.Empty<string>());
                        return await ProbeMediaInternalAsync(tempItem.Media, cancellationToken);
                    }
                    finally
                    {
                        if (tempItem is not null)
                        {
                            _playerService.DisposePlaybackItem(tempItem);
                        }
                    }
            }
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

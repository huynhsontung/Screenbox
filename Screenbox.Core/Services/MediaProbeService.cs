using System;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Windows.Storage;

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
    /// <param name="item">The playback item to probe.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if probing was canceled or failed.</returns>
    public Task<MediaInfo?> ProbeMediaAsync(
        PlaybackItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return ProbeMediaAsync(item.Media, cancellationToken);
    }

    /// <summary>
    /// Probes LibVLC media properties from the specified <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The storage file to probe.</param>
    /// <param name="options">Optional LibVLC media options.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if probing was canceled or failed.</returns>
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public Task<MediaInfo?> ProbeMediaAsync(
        StorageFile file,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        return ProbeSourceAsync(file, options, cancellationToken);
    }

    /// <summary>
    /// Probes LibVLC media properties from the specified <paramref name="uri"/>.
    /// </summary>
    /// <param name="uri">The media URI to probe.</param>
    /// <param name="options">Optional LibVLC media options.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if probing was canceled or failed.</returns>
    public Task<MediaInfo?> ProbeMediaAsync(
        Uri uri,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return ProbeSourceAsync(uri, options, cancellationToken);
    }

    /// <inheritdoc/>
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async Task<MediaInfo?> ProbeMediaAsync(
        object source,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        if (source is null)
        {
            return null;
        }

        return source switch
        {
            PlaybackItem existingItem => await ProbeMediaAsync(existingItem, cancellationToken),
            Media existingMedia => await ProbeMediaAsync(existingMedia, cancellationToken),
            StorageFile file => await ProbeMediaAsync(file, options, cancellationToken),
            Uri uri => await ProbeMediaAsync(uri, options, cancellationToken),
            _ => await ProbeSourceAsync(source, options, cancellationToken)
        };
    }

    private async Task<MediaInfo?> ProbeSourceAsync(
        object source,
        string[]? options,
        CancellationToken cancellationToken)
    {
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
                options ?? []);
            return await ProbeMediaAsync(tempItem.Media, cancellationToken);
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
            if (tempItem is not null)
            {
                _playerService.DisposePlaybackItem(tempItem);
            }
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

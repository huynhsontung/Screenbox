using System;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Services;

/// <summary>
/// Playback metadata service backed by LibVLC media parsing.
/// </summary>
public sealed class VlcMetadataService : IPlaybackMetadataService
{
    private static readonly TimeSpan DefaultParseTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<VlcMetadataService> _logger;

    public VlcMetadataService(ILogger<VlcMetadataService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves LibVLC media properties from the specified <paramref name="media"/> instance.
    /// </summary>
    /// <param name="media">The LibVLC media to inspect.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if parsing was canceled or failed.</returns>
    public async Task<MediaInfo?> GetMediaInfoAsync(
        Media media,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);

        try
        {
            return await GetMediaInfoInternalAsync(media, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract media metadata from LibVLC media.");
            return null;
        }
    }

    /// <summary>
    /// Retrieves LibVLC media properties from the specified <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The VLC playback item to inspect.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if parsing was canceled or failed.</returns>
    public Task<MediaInfo?> GetMediaInfoAsync(
        VlcPlaybackItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetMediaInfoAsync(item.Media, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<MediaInfo?> GetMediaInfoAsync(
        IPlaybackItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is not VlcPlaybackItem vlcItem)
        {
            throw new NotSupportedException("Only VlcPlaybackItem is supported.");
        }

        return GetMediaInfoAsync(vlcItem, cancellationToken);
    }

    private static async Task<MediaInfo?> GetMediaInfoInternalAsync(Media media, CancellationToken cancellationToken)
    {
        if (!media.IsParsed || media.ParsedStatus != MediaParsedStatus.Done)
        {
            await media.ParseAsync(DefaultParseTimeout, cancellationToken);
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

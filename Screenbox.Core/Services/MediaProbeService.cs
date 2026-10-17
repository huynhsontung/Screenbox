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
using WinRT;

namespace Screenbox.Core.Services;

/// <summary>
/// Default <see cref="IMediaProbeService"/> implementation backed by LibVLC media parsing.
/// </summary>
public sealed class MediaProbeService : IMediaProbeService
{
    private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(10);

    private readonly PlayerContext _playerContext;
    private readonly IPlayerService _playerService;
    private readonly IFilesService _filesService;
    private readonly ILogger<MediaProbeService> _logger;

    public MediaProbeService(
        PlayerContext playerContext,
        IPlayerService playerService,
        IFilesService filesService,
        ILogger<MediaProbeService> logger)
    {
        _playerContext = playerContext;
        _playerService = playerService;
        _filesService = filesService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MediaInfo?> ProbeMediaAsync(
        object source,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        Task<MediaInfo?> fileInfoTask = TryGetFileMediaInfoAsync(source);
        Task<MediaInfo?> parsedInfoTask = ProbeVlcSourceAsync(source, options, cancellationToken);

        await Task.WhenAll(fileInfoTask, parsedInfoTask);

        MediaInfo? fileInfo = await fileInfoTask;
        MediaInfo? parsedInfo = await parsedInfoTask;

        return (fileInfo, parsedInfo) switch
        {
            (not null, not null) => MediaInfo.Merge(fileInfo, parsedInfo),
            (not null, null) => fileInfo,
            (null, not null) => parsedInfo,
            (null, null) => null
        };
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private async Task<MediaInfo?> TryGetFileMediaInfoAsync(object source)
    {
        StorageFile? file = source switch
        {
            StorageFile sf => sf,
            PlaybackItem { OriginalSource: StorageFile sf } => sf,
            PlaybackItem { OriginalSource: Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri } =>
                await FilesHelpers.TryGetFileFromPathAsync(uri.OriginalString),
            Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri =>
                await FilesHelpers.TryGetFileFromPathAsync(uri.OriginalString),
            _ => null
        };

        return file is not null
            ? await _filesService.GetMediaInfoAsync(file)
            : null;
    }

    private async Task<MediaInfo?> ProbeVlcSourceAsync(
        object source,
        string[]? options,
        CancellationToken cancellationToken)
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
                    if (_playerContext.MediaPlayer is not VlcMediaPlayer vlcMediaPlayer ||
                        _playerService is not PlayerService playerService)
                    {
                        return null;
                    }

                    Media? tempMedia = null;
                    try
                    {
                        tempMedia = playerService.CreateMedia(
                            vlcMediaPlayer,
                            source,
                            options ?? Array.Empty<string>());
                        return await ProbeMediaInternalAsync(tempMedia, cancellationToken);
                    }
                    finally
                    {
                        if (tempMedia is not null)
                        {
                            playerService.DisposeMedia(tempMedia);
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

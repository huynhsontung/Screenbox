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
        Task<StorageFile?> fileTask = TryResolveStorageFileAsync(source);
        Task<MediaInfo?> parsedInfoTask = ProbeVlcSourceAsync(source, options, cancellationToken);

        await Task.WhenAll(fileTask, parsedInfoTask);

        StorageFile? file = await fileTask;
        MediaInfo? parsedInfo = await parsedInfoTask;

        if (file is null)
        {
            return parsedInfo;
        }

        if (parsedInfo is null)
        {
            return await _filesService.GetMediaInfoAsync(file);
        }

        if (file.IsAvailable)
        {
            try
            {
                var basicProperties = await file.GetBasicPropertiesAsync();
                parsedInfo.Size = basicProperties.Size;
                parsedInfo.DateModified = basicProperties.DateModified;
            }
            catch (Exception)
            {
                // Ignore transient file property failures after media has been parsed.
            }
        }

        return parsedInfo;
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private static async Task<StorageFile?> TryResolveStorageFileAsync(object source)
    {
        return source switch
        {
            StorageFile sf => sf,
            PlaybackItem { OriginalSource: StorageFile sf } => sf,
            PlaybackItem { OriginalSource: Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri } =>
                await FilesHelpers.TryGetFileFromPathAsync(uri.OriginalString),
            Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri =>
                await FilesHelpers.TryGetFileFromPathAsync(uri.OriginalString),
            _ => null
        };
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

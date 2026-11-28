using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Windows.Storage;
using Windows.Storage.FileProperties;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;

namespace Screenbox.Core.Services;

/// <summary>
/// Fast source metadata service backed by Windows Storage file properties (WinRT).
/// </summary>
public sealed class StorageMetadataService : ISourceMetadataService
{
    private static readonly string[] SummaryPropertyKeys =
    [
        SystemProperties.Music.Artist,
        SystemProperties.Media.Duration
    ];

    private readonly IFilesService _filesService;
    private readonly ILogger<StorageMetadataService> _logger;

    public StorageMetadataService(IFilesService filesService, ILogger<StorageMetadataService> logger)
    {
        _filesService = filesService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves Windows Storage properties from the specified <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The storage file to inspect.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if the operation was canceled.</returns>
    public async Task<MediaInfo?> GetMediaInfoAsync(
        StorageFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        MediaPlaybackType mediaType = FilesHelpers.GetMediaTypeForFile(file);
        if (!file.IsAvailable)
        {
            return new MediaInfo(mediaType);
        }

        try
        {
            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            cancellationToken.ThrowIfCancellationRequested();

            switch (mediaType)
            {
                case MediaPlaybackType.Video:
                    VideoProperties videoProperties = await file.Properties.GetVideoPropertiesAsync();
                    return new MediaInfo(basicProperties, videoProperties);

                case MediaPlaybackType.Music:
                    MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();
                    return new MediaInfo(basicProperties, musicProperties);
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e) when (IsExpectedStoragePropertiesHResult(e.HResult))
        {
            // Expected transient WinRT failures while querying StorageFile properties:
            //   0x800706BA RPC_S_SERVER_UNAVAILABLE - the RPC server is unavailable.
            //   0x8000000E E_ILLEGAL_METHOD_CALL    - file's property provider isn't ready
            //                                         (file became unavailable after IsAvailable,
            //                                          app suspended, or item disposed).
            //   0x80070490 ERROR_NOT_FOUND          - element not found.
            // These are already handled by returning a default MediaInfo; don't report to Sentry.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load media metadata for file type '{FileType}'.", file.FileType);
        }

        return new MediaInfo(mediaType);
    }

    /// <summary>
    /// Retrieves Windows Storage properties from the specified <paramref name="fileSource"/>.
    /// </summary>
    /// <param name="fileSource">The file media source to inspect.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if the file could not be resolved or the operation was canceled.</returns>
    public async Task<MediaInfo?> GetMediaInfoAsync(
        IFileMediaSource fileSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileSource);

        if (await _filesService.ResolveFileAsync(fileSource) is not { } file)
        {
            return null;
        }

        return await GetMediaInfoAsync(file, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MediaInfo?> GetMediaInfoAsync(
        IMediaSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IFileMediaSource fileSource)
        {
            return await GetMediaInfoAsync(fileSource, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Retrieves lightweight display summary properties (artist and duration) from <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The storage file to inspect.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The extracted artist name (if any) and duration.</returns>
    public async Task<(string? Artist, TimeSpan Duration)> GetSummaryPropertiesAsync(
        StorageFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (cancellationToken.IsCancellationRequested || !file.IsAvailable)
        {
            return (null, TimeSpan.Zero);
        }

        try
        {
            IDictionary<string, object> additionalProperties =
                await file.Properties.RetrievePropertiesAsync(SummaryPropertyKeys);
            if (cancellationToken.IsCancellationRequested)
            {
                return (null, TimeSpan.Zero);
            }

            string? artist = additionalProperties.TryGetValue(SystemProperties.Music.Artist, out object? artistObj) &&
                             artistObj is string[] { Length: > 0 } contributingArtists
                ? string.Join(", ", contributingArtists)
                : null;

            TimeSpan duration = additionalProperties.TryGetValue(SystemProperties.Media.Duration, out object? durationObj) &&
                                durationObj is ulong ticks and > 0
                ? TimeSpan.FromTicks((long)ticks)
                : TimeSpan.Zero;

            return (artist, duration);
        }
        catch (OperationCanceledException)
        {
            return (null, TimeSpan.Zero);
        }
        catch (Exception e) when (IsExpectedStoragePropertiesHResult(e.HResult))
        {
            // Expected transient WinRT failures while querying StorageFile properties.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load summary properties for file type '{FileType}'.", file.FileType);
        }

        return (null, TimeSpan.Zero);
    }

    /// <inheritdoc/>
    public async Task<(string? Artist, TimeSpan Duration)> GetSummaryPropertiesAsync(
        IMediaSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (cancellationToken.IsCancellationRequested || source is not IFileMediaSource fileSource)
        {
            return (null, TimeSpan.Zero);
        }

        StorageFile? file = await _filesService.ResolveFileAsync(fileSource);
        if (file is null)
        {
            return (null, TimeSpan.Zero);
        }

        return await GetSummaryPropertiesAsync(file, cancellationToken);
    }

    private static bool IsExpectedStoragePropertiesHResult(int hresult)
    {
        const int RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
        const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000E);
        const int ERROR_NOT_FOUND = unchecked((int)0x80070490);
        return hresult == RPC_S_SERVER_UNAVAILABLE
               || hresult == E_ILLEGAL_METHOD_CALL
               || hresult == ERROR_NOT_FOUND;
    }
}

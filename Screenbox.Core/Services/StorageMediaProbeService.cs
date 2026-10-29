using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;

namespace Screenbox.Core.Services;

/// <summary>
/// Fast media probe service backed by Windows Storage file properties (WinRT).
/// </summary>
public sealed class StorageMediaProbeService : IMediaProbeService
{
    private readonly ILogger<StorageMediaProbeService> _logger;

    public StorageMediaProbeService(ILogger<StorageMediaProbeService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Probes Windows Storage properties from the specified <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The storage file to probe.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if probing was canceled.</returns>
    public async Task<MediaInfo?> ProbeMediaAsync(
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
    /// Probes Windows Storage properties from the specified <paramref name="fileSource"/>.
    /// </summary>
    /// <param name="fileSource">The file media source to probe.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="MediaInfo"/>, or <see langword="null"/> if the file could not be resolved or probing was canceled.</returns>
    public async Task<MediaInfo?> ProbeMediaAsync(
        IFileMediaSource fileSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileSource);

        if (await fileSource.GetStorageFileAsync() is not { } file)
        {
            return null;
        }

        return await ProbeMediaAsync(file, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MediaInfo?> ProbeMediaAsync(
        IMediaSource source,
        string[]? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IFileMediaSource fileSource)
        {
            return await ProbeMediaAsync(fileSource, cancellationToken);
        }

        return null;
    }

    /// <inheritdoc/>
    public Task<MediaInfo?> ProbeMediaAsync(
        IPlaybackItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return ProbeMediaAsync(item.Source, null, cancellationToken);
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

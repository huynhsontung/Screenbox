using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Screenbox.Core.MediaSources;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service that extracts embedded artwork and file thumbnails via Windows Storage APIs.
/// </summary>
public sealed class MediaThumbnailService : IMediaThumbnailService
{
    private readonly ILogger<MediaThumbnailService> _logger;

    public MediaThumbnailService(ILogger<MediaThumbnailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a thumbnail image stream from the specified <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The storage file to inspect.</param>
    /// <param name="requestedSize">The requested thumbnail dimension in pixels.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IRandomAccessStream"/> containing the thumbnail image if available;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    public async Task<IRandomAccessStream?> GetThumbnailStreamAsync(
        StorageFile file,
        uint requestedSize = 1280,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (cancellationToken.IsCancellationRequested || !file.IsAvailable)
        {
            return null;
        }

        try
        {
            // Use SingleItem mode to retrieve embedded album art with original aspect ratio.
            // https://learn.microsoft.com/windows/apps/develop/files/thumbnails
            StorageItemThumbnail? source = await file.GetThumbnailAsync(
                ThumbnailMode.SingleItem,
                requestedSize,
                ThumbnailOptions.UseCurrentScale);

            if (cancellationToken.IsCancellationRequested)
            {
                source?.Dispose();
                return null;
            }

            if (source is { Type: ThumbnailType.Image })
            {
                return source;
            }

            source?.Dispose();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e) when (IsExpectedThumbnailHResult(e.HResult))
        {
            // Expected transient WinRT failures while querying thumbnails:
            //   0x8000000A E_PENDING              - data necessary to complete this operation is not yet available.
            //   0x800706BA RPC_S_SERVER_UNAVAILABLE - the RPC server is unavailable.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load thumbnail for file type '{FileType}'.", file.FileType);
        }

        return null;
    }

    /// <summary>
    /// Retrieves a thumbnail image stream from the specified <paramref name="fileSource"/>.
    /// </summary>
    /// <param name="fileSource">The file media source to inspect.</param>
    /// <param name="requestedSize">The requested thumbnail dimension in pixels.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IRandomAccessStream"/> containing the thumbnail image if available;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    public async Task<IRandomAccessStream?> GetThumbnailStreamAsync(
        IFileMediaSource fileSource,
        uint requestedSize = 1280,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileSource);

        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        StorageFile? file = fileSource.StorageFile ?? await fileSource.GetStorageFileAsync();
        if (file is null)
        {
            return null;
        }

        return await GetThumbnailStreamAsync(file, requestedSize, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IRandomAccessStream?> GetThumbnailStreamAsync(
        IMediaSource source,
        uint requestedSize = 1280,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IFileMediaSource fileSource)
        {
            return await GetThumbnailStreamAsync(fileSource, requestedSize, cancellationToken);
        }

        return null;
    }

    private static bool IsExpectedThumbnailHResult(int hresult)
    {
        const int E_PENDING = unchecked((int)0x8000000A);
        const int RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
        return hresult == E_PENDING || hresult == RPC_S_SERVER_UNAVAILABLE;
    }
}

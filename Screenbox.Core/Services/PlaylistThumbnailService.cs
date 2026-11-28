#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Screenbox.Core.Services;

/// <summary>
/// Generates full-bleed JPEG playlist thumbnail collages and caches them under
/// <c>LocalCacheFolder\PlaylistThumbnails</c>.
/// </summary>
public sealed class PlaylistThumbnailService : IPlaylistThumbnailService
{
    private const int ThumbnailCollageSize = 400;
    private const int MaxArtworkCount = 4;
    internal const int MaxScanCount = 32;
    private const int BytesPerPixel = 4;
    private const float JpegQuality = 0.85f;
    private const string CacheFolderName = "PlaylistThumbnails";

    private readonly record struct CollageSlot(int X, int Y, int Width, int Height);

    /// <summary>
    /// Gets or sets the directory used to store cached playlist thumbnails.
    /// Defaults to <c>ApplicationData.Current.LocalCacheFolder\PlaylistThumbnails</c> if not specified.
    /// </summary>
    public string CacheFolderPath { get; set; } = string.Empty;

    private readonly IMediaThumbnailService _mediaThumbnailService;
    private readonly ILogger<PlaylistThumbnailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistThumbnailService"/> class.
    /// </summary>
    /// <param name="mediaThumbnailService">The service used to extract thumbnail streams from media sources.</param>
    /// <param name="logger">The logger instance.</param>
    public PlaylistThumbnailService(
        IMediaThumbnailService mediaThumbnailService,
        ILogger<PlaylistThumbnailService> logger)
    {
        _mediaThumbnailService = mediaThumbnailService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> ReadThumbnailAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
            return null;

        string path = GetThumbnailPath(playlistId);
        if (!File.Exists(path))
            return null;

        try
        {
            return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]?> BuildAndCacheThumbnailAsync(
        string playlistId,
        IReadOnlyList<PlaylistThumbnailItem> items,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
            return null;

        if (items.Count == 0)
        {
            await DeleteThumbnailAsync(playlistId).ConfigureAwait(false);
            return null;
        }

        // Run thumbnail fetching, WIC decoding/encoding, pixel blitting, and file I/O off the UI thread.
        return await Task.Run(async () =>
        {
            List<(IRandomAccessStream Stream, BitmapDecoder Decoder)> sources = new(MaxArtworkCount);

            try
            {
                await CollectDistinctArtworksAsync(
                    items,
                    sources,
                    TryOpenDecoderAsync,
                    MaxArtworkCount,
                    MaxScanCount,
                    cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (sources.Count == 0)
                {
                    await DeleteThumbnailAsync(playlistId).ConfigureAwait(false);
                    return null;
                }

                CollageSlot[] layout = GetCollageLayout(sources.Count, ThumbnailCollageSize);
                List<(CollageSlot Slot, byte[] Pixels)> cells = new(layout.Length);

                for (int i = 0; i < layout.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[]? pixels = await DecodeSlotPixelsAsync(
                        sources[i].Decoder,
                        layout[i].Width,
                        layout[i].Height,
                        cancellationToken).ConfigureAwait(false);

                    if (pixels is not null)
                    {
                        cells.Add((layout[i], pixels));
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (cells.Count == 0)
                {
                    await DeleteThumbnailAsync(playlistId).ConfigureAwait(false);
                    return null;
                }

                // If any decoder failed partway through, recompute layout for the surviving cells so no gap remains.
                if (cells.Count < layout.Length)
                {
                    CollageSlot[] fallbackLayout = GetCollageLayout(cells.Count, ThumbnailCollageSize);
                    List<(CollageSlot Slot, byte[] Pixels)> refitted = new(fallbackLayout.Length);
                    int survivingIndex = 0;

                    for (int i = 0; i < sources.Count && survivingIndex < fallbackLayout.Length; i++)
                    {
                        byte[]? pixels = await DecodeSlotPixelsAsync(
                            sources[i].Decoder,
                            fallbackLayout[survivingIndex].Width,
                            fallbackLayout[survivingIndex].Height,
                            cancellationToken).ConfigureAwait(false);

                        if (pixels is not null)
                        {
                            refitted.Add((fallbackLayout[survivingIndex], pixels));
                            survivingIndex++;
                        }
                    }

                    if (refitted.Count == 0)
                    {
                        await DeleteThumbnailAsync(playlistId).ConfigureAwait(false);
                        return null;
                    }

                    cells = refitted;
                }

                byte[] collagePixels = CompositeCollagePixels(ThumbnailCollageSize, cells);
                byte[] encodedBytes = await EncodeJpegAsync(collagePixels, ThumbnailCollageSize, cancellationToken)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                await WriteThumbnailAtomicallyAsync(playlistId, encodedBytes, cancellationToken).ConfigureAwait(false);

                return encodedBytes;
            }
            finally
            {
                foreach (var source in sources)
                {
                    source.Stream.Dispose();
                }
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DeleteThumbnailAsync(string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
            return Task.CompletedTask;

        try
        {
            string path = GetThumbnailPath(playlistId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Already removed.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete cached thumbnail for playlist '{PlaylistId}'.", playlistId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Scans up to <paramref name="maxScanCount"/> items and collects up to <paramref name="maxArtworkCount"/>
    /// readable artworks into <paramref name="destination"/>, deduplicating albums and media locations only after
    /// an item supplies usable artwork so unreadable early tracks do not block later tracks on the same album.
    /// </summary>
    private static async Task CollectDistinctArtworksAsync<TArtwork>(
        IReadOnlyList<PlaylistThumbnailItem> items,
        List<TArtwork> destination,
        Func<PlaylistThumbnailItem, CancellationToken, Task<TArtwork?>> tryOpenArtworkAsync,
        int maxArtworkCount = MaxArtworkCount,
        int maxScanCount = MaxScanCount,
        CancellationToken cancellationToken = default)
        where TArtwork : struct
    {
        int scanLimit = Math.Min(items.Count, maxScanCount);
        HashSet<string> selectedKeys = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> failedLocations = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < scanLimit; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PlaylistThumbnailItem item = items[i];
            string? dedupeKey = GetArtworkDeduplicationKey(item);
            if (dedupeKey is not null && selectedKeys.Contains(dedupeKey))
                continue;

            string location = item.Source.Location;
            if (!string.IsNullOrWhiteSpace(location) && failedLocations.Contains(location))
                continue;

            TArtwork? opened = await tryOpenArtworkAsync(item, cancellationToken).ConfigureAwait(false);
            if (opened is null)
            {
                if (!string.IsNullOrWhiteSpace(location))
                {
                    failedLocations.Add(location);
                }

                continue;
            }

            if (dedupeKey is not null)
            {
                selectedKeys.Add(dedupeKey);
            }

            destination.Add(opened.Value);
            if (destination.Count == maxArtworkCount)
                break;
        }
    }

    /// <summary>
    /// Computes the full-bleed collage slot rectangles for 1 to 4 artworks on a square canvas:
    /// 1 = full bleed, 2 = side-by-side vertical halves, 3 = 1+2 split (tall left half + two right squares),
    /// 4 = 2x2 quadrants.
    /// </summary>
    private static CollageSlot[] GetCollageLayout(int artworkCount, int canvasSize = ThumbnailCollageSize)
    {
        if (artworkCount <= 0 || canvasSize <= 0)
            return [];

        int half = canvasSize / 2;
        int remainder = canvasSize - half;

        return artworkCount switch
        {
            1 => [new CollageSlot(0, 0, canvasSize, canvasSize)],
            2 =>
            [
                new CollageSlot(0, 0, half, canvasSize),
                new CollageSlot(half, 0, remainder, canvasSize)
            ],
            3 =>
            [
                new CollageSlot(0, 0, half, canvasSize),
                new CollageSlot(half, 0, remainder, half),
                new CollageSlot(half, half, remainder, remainder)
            ],
            _ =>
            [
                new CollageSlot(0, 0, half, half),
                new CollageSlot(half, 0, remainder, half),
                new CollageSlot(0, half, half, remainder),
                new CollageSlot(half, half, remainder, remainder)
            ]
        };
    }

    /// <summary>
    /// Composites decoded BGRA8 cell pixel buffers into a single square BGRA8 canvas buffer.
    /// </summary>
    private static byte[] CompositeCollagePixels(
        int canvasSize,
        IReadOnlyList<(CollageSlot Slot, byte[] Pixels)> cells)
    {
        byte[] collage = new byte[canvasSize * canvasSize * BytesPerPixel];

        foreach (var (slot, cellPixels) in cells)
        {
            int bytesPerCellRow = slot.Width * BytesPerPixel;
            for (int row = 0; row < slot.Height; row++)
            {
                int sourceOffset = row * bytesPerCellRow;
                int destinationOffset = ((slot.Y + row) * canvasSize + slot.X) * BytesPerPixel;
                cellPixels.AsSpan(sourceOffset, bytesPerCellRow)
                    .CopyTo(collage.AsSpan(destinationOffset, bytesPerCellRow));
            }
        }

        return collage;
    }

    private async Task WriteThumbnailAtomicallyAsync(
        string playlistId,
        byte[] encodedBytes,
        CancellationToken cancellationToken)
    {
        string folderPath = ResolveCacheFolderPath();
        Directory.CreateDirectory(folderPath);

        string finalPath = GetThumbnailPath(playlistId);
        string safeFileName = Path.GetFileName(playlistId);
        string tempPath = Path.Combine(folderPath, $"{safeFileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllBytesAsync(tempPath, encodedBytes, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, finalPath, overwrite: true);
        }
        catch
        {
            TryDeleteFileQuietly(tempPath);
            throw;
        }
    }

    private string GetThumbnailPath(string playlistId)
    {
        string folderPath = ResolveCacheFolderPath();
        string safeFileName = Path.GetFileName(playlistId);

        return Path.Combine(folderPath, $"{safeFileName}.jpg");
    }

    private string ResolveCacheFolderPath()
    {
        if (!string.IsNullOrEmpty(CacheFolderPath))
            return CacheFolderPath;

        string defaultPath = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, CacheFolderName);
        CacheFolderPath = defaultPath;

        return defaultPath;
    }

    private static string? GetArtworkDeduplicationKey(PlaylistThumbnailItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.GroupKey))
            return item.GroupKey;

        return !string.IsNullOrWhiteSpace(item.Source.Location) ? "loc:" + item.Source.Location : null;
    }

    private async Task<(IRandomAccessStream Stream, BitmapDecoder Decoder)?> TryOpenDecoderAsync(
        PlaylistThumbnailItem item,
        CancellationToken cancellationToken)
    {
        IRandomAccessStream? stream = null;
        try
        {
            stream = await _mediaThumbnailService.GetThumbnailStreamAsync(
                item.Source,
                ThumbnailCollageSize,
                cancellationToken).ConfigureAwait(false);
            if (stream is null)
                return null;

            cancellationToken.ThrowIfCancellationRequested();

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream).AsTask(cancellationToken).ConfigureAwait(false);
            if (decoder.OrientedPixelWidth == 0 || decoder.OrientedPixelHeight == 0)
            {
                stream.Dispose();
                return null;
            }

            return (stream, decoder);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stream?.Dispose();
            return null;
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }

    private static async Task<byte[]?> DecodeSlotPixelsAsync(
        BitmapDecoder decoder,
        int targetWidth,
        int targetHeight,
        CancellationToken cancellationToken)
    {
        try
        {
            uint pixelWidth = decoder.OrientedPixelWidth;
            uint pixelHeight = decoder.OrientedPixelHeight;
            if (pixelWidth == 0 || pixelHeight == 0)
                return null;

            uint width = (uint)targetWidth;
            uint height = (uint)targetHeight;

            // Scale so the image covers the target slot (UniformToFill), then center-crop the overflow.
            double scale = Math.Max((double)width / pixelWidth, (double)height / pixelHeight);
            uint scaledWidth = Math.Max(width, (uint)Math.Round(pixelWidth * scale));
            uint scaledHeight = Math.Max(height, (uint)Math.Round(pixelHeight * scale));

            BitmapTransform transform = new()
            {
                ScaledWidth = scaledWidth,
                ScaledHeight = scaledHeight,
                Bounds = new BitmapBounds
                {
                    X = (scaledWidth - width) / 2,
                    Y = (scaledHeight - height) / 2,
                    Width = width,
                    Height = height,
                },
                InterpolationMode = BitmapInterpolationMode.Fant,
            };

            PixelDataProvider pixelProvider = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage).AsTask(cancellationToken).ConfigureAwait(false);

            return pixelProvider.DetachPixelData();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static async Task<byte[]> EncodeJpegAsync(
        byte[] pixels,
        int canvasSize,
        CancellationToken cancellationToken)
    {
        using InMemoryRandomAccessStream stream = new();
        BitmapPropertySet encoderProperties = new()
        {
            { "ImageQuality", new BitmapTypedValue(JpegQuality, Windows.Foundation.PropertyType.Single) }
        };

        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(
            BitmapEncoder.JpegEncoderId,
            stream,
            encoderProperties).AsTask(cancellationToken).ConfigureAwait(false);

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            width: (uint)canvasSize,
            height: (uint)canvasSize,
            dpiX: 96.0,
            dpiY: 96.0,
            pixels);

        await encoder.FlushAsync().AsTask(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = new byte[stream.Size];
        stream.Seek(0);
        await stream.ReadAsync(buffer.AsBuffer(), (uint)buffer.Length, InputStreamOptions.None)
            .AsTask(cancellationToken).ConfigureAwait(false);

        return buffer;
    }

    private static void TryDeleteFileQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup for temporary files.
        }
    }
}

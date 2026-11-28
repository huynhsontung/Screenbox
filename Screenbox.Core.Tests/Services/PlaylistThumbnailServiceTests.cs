using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Screenbox.Core.Tests.Services;

public class PlaylistThumbnailServiceTests
{
    private sealed class FakeMediaThumbnailService : IMediaThumbnailService
    {
        private readonly Func<IMediaSource, uint, CancellationToken, Task<IRandomAccessStream?>> _handler;

        public FakeMediaThumbnailService(Func<IMediaSource, uint, CancellationToken, Task<IRandomAccessStream?>> handler)
        {
            _handler = handler;
        }

        public Task<IRandomAccessStream?> GetThumbnailStreamAsync(
            IMediaSource source,
            uint requestedSize = 1280,
            CancellationToken cancellationToken = default) =>
            _handler(source, requestedSize, cancellationToken);
    }

    private static PlaylistThumbnailItem CreateItem(string location, string? groupKey = null) =>
        new(new UriMediaSource(new Uri(location)), groupKey);

    private static async Task<IRandomAccessStream> CreateSolidPngStreamAsync(
        uint width,
        uint height,
        (byte R, byte G, byte B) color)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = color.B;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.R;
            pixels[i + 3] = 255;
        }

        InMemoryRandomAccessStream stream = new();
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            width,
            height,
            dpiX: 96.0,
            dpiY: 96.0,
            pixels);
        await encoder.FlushAsync();
        stream.Seek(0);

        return stream;
    }

    private static async Task<(uint Width, uint Height, byte[] Pixels)> DecodeBgraPixelsAsync(byte[] encodedBytes)
    {
        using MemoryStream memoryStream = new(encodedBytes);
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(memoryStream.AsRandomAccessStream());
        PixelDataProvider provider = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            new BitmapTransform(),
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.DoNotColorManage);

        return (decoder.PixelWidth, decoder.PixelHeight, provider.DetachPixelData());
    }

    private static (byte R, byte G, byte B) GetRgbPixel(byte[] pixels, uint width, int x, int y)
    {
        int offset = (y * (int)width + x) * 4;
        return (pixels[offset + 2], pixels[offset + 1], pixels[offset]);
    }

    private static async Task AssertColorNearAsync(
        (byte R, byte G, byte B) actual,
        (byte R, byte G, byte B) expected,
        int tolerance = 15)
    {
        await Assert.That(Math.Abs(actual.R - expected.R)).IsLessThanOrEqualTo(tolerance);
        await Assert.That(Math.Abs(actual.G - expected.G)).IsLessThanOrEqualTo(tolerance);
        await Assert.That(Math.Abs(actual.B - expected.B)).IsLessThanOrEqualTo(tolerance);
    }

    [Test]
    public async Task BuildAndCacheThumbnailAsync_OneToFourArtworks_ProducesExpectedFullBleedJpegLayouts()
    {
        using var fixture = new TestDirectoryFixture();
        (byte R, byte G, byte B) red = (220, 40, 40);
        (byte R, byte G, byte B) green = (40, 200, 60);
        (byte R, byte G, byte B) blue = (40, 80, 220);
        (byte R, byte G, byte B) yellow = (230, 210, 40);

        var v1 = CreateItem("https://example.com/v1.mp4");
        var v2 = CreateItem("https://example.com/v2.mp4");
        var v3 = CreateItem("https://example.com/v3.mp4");
        var v4 = CreateItem("https://example.com/v4.mp4");

        var colorsByLocation = new Dictionary<string, (uint W, uint H, (byte R, byte G, byte B) Color)>
        {
            [v1.Source.Location] = (600, 200, red),
            [v2.Source.Location] = (200, 200, green),
            [v3.Source.Location] = (200, 200, blue),
            [v4.Source.Location] = (200, 200, yellow)
        };

        var mediaThumbnails = new FakeMediaThumbnailService(async (source, _, _) =>
        {
            var spec = colorsByLocation[source.Location];
            return await CreateSolidPngStreamAsync(spec.W, spec.H, spec.Color);
        });

        var service = new PlaylistThumbnailService(mediaThumbnails, NullLogger<PlaylistThumbnailService>.Instance)
        {
            CacheFolderPath = fixture.DirectoryPath
        };

        byte[]? bytes1 = await service.BuildAndCacheThumbnailAsync(Guid.NewGuid().ToString(), new[] { v1 });
        await Assert.That(bytes1).IsNotNull();
        var decoded1 = await DecodeBgraPixelsAsync(bytes1!);
        await Assert.That(decoded1.Width).IsEqualTo(400u);
        await Assert.That(decoded1.Height).IsEqualTo(400u);
        await AssertColorNearAsync(GetRgbPixel(decoded1.Pixels, decoded1.Width, 100, 100), red);
        await AssertColorNearAsync(GetRgbPixel(decoded1.Pixels, decoded1.Width, 300, 100), red);
        await AssertColorNearAsync(GetRgbPixel(decoded1.Pixels, decoded1.Width, 100, 300), red);
        await AssertColorNearAsync(GetRgbPixel(decoded1.Pixels, decoded1.Width, 300, 300), red);

        byte[]? bytes2 = await service.BuildAndCacheThumbnailAsync(Guid.NewGuid().ToString(), new[] { v1, v2 });
        await Assert.That(bytes2).IsNotNull();
        var decoded2 = await DecodeBgraPixelsAsync(bytes2!);
        await Assert.That(decoded2.Width).IsEqualTo(400u);
        await Assert.That(decoded2.Height).IsEqualTo(400u);
        await AssertColorNearAsync(GetRgbPixel(decoded2.Pixels, decoded2.Width, 100, 200), red);
        await AssertColorNearAsync(GetRgbPixel(decoded2.Pixels, decoded2.Width, 300, 200), green);

        byte[]? bytes3 = await service.BuildAndCacheThumbnailAsync(Guid.NewGuid().ToString(), new[] { v1, v2, v3 });
        await Assert.That(bytes3).IsNotNull();
        var decoded3 = await DecodeBgraPixelsAsync(bytes3!);
        await Assert.That(decoded3.Width).IsEqualTo(400u);
        await Assert.That(decoded3.Height).IsEqualTo(400u);
        await AssertColorNearAsync(GetRgbPixel(decoded3.Pixels, decoded3.Width, 100, 100), red);
        await AssertColorNearAsync(GetRgbPixel(decoded3.Pixels, decoded3.Width, 100, 300), red);
        await AssertColorNearAsync(GetRgbPixel(decoded3.Pixels, decoded3.Width, 300, 100), green);
        await AssertColorNearAsync(GetRgbPixel(decoded3.Pixels, decoded3.Width, 300, 300), blue);

        byte[]? bytes4 = await service.BuildAndCacheThumbnailAsync(Guid.NewGuid().ToString(), new[] { v1, v2, v3, v4 });
        await Assert.That(bytes4).IsNotNull();
        var decoded4 = await DecodeBgraPixelsAsync(bytes4!);
        await Assert.That(decoded4.Width).IsEqualTo(400u);
        await Assert.That(decoded4.Height).IsEqualTo(400u);
        await AssertColorNearAsync(GetRgbPixel(decoded4.Pixels, decoded4.Width, 100, 100), red);
        await AssertColorNearAsync(GetRgbPixel(decoded4.Pixels, decoded4.Width, 300, 100), green);
        await AssertColorNearAsync(GetRgbPixel(decoded4.Pixels, decoded4.Width, 100, 300), blue);
        await AssertColorNearAsync(GetRgbPixel(decoded4.Pixels, decoded4.Width, 300, 300), yellow);
    }

    [Test]
    public async Task BuildAndCacheThumbnailAsync_UnreadableFirstGroupItem_FallsBackToLaterItemAndDeduplicatesGroupsAndLocations()
    {
        using var fixture = new TestDirectoryFixture();
        (byte R, byte G, byte B) red = (220, 40, 40);
        (byte R, byte G, byte B) green = (40, 200, 60);
        (byte R, byte G, byte B) blue = (40, 80, 220);

        var albumATrack1 = CreateItem("https://example.com/a1.mp3", groupKey: "album:a");
        var albumATrack1Dup = CreateItem("https://example.com/a1.mp3", groupKey: "album:a");
        var albumATrack2 = CreateItem("https://example.com/a2.mp3", groupKey: "album:a");
        var albumATrack3 = CreateItem("https://example.com/a3.mp3", groupKey: "album:a");
        var video1 = CreateItem("https://example.com/v1.mp4");
        var video1Dup = CreateItem("https://example.com/v1.mp4");

        var probedSources = new List<IMediaSource>();
        var mediaThumbnails = new FakeMediaThumbnailService(async (source, _, _) =>
        {
            probedSources.Add(source);
            if (ReferenceEquals(source, albumATrack1.Source) || ReferenceEquals(source, albumATrack1Dup.Source))
                return null;
            if (ReferenceEquals(source, albumATrack2.Source))
                return await CreateSolidPngStreamAsync(200, 200, red);
            if (ReferenceEquals(source, albumATrack3.Source))
                return await CreateSolidPngStreamAsync(200, 200, green);
            if (ReferenceEquals(source, video1.Source) || ReferenceEquals(source, video1Dup.Source))
                return await CreateSolidPngStreamAsync(200, 200, blue);

            return null;
        });

        var service = new PlaylistThumbnailService(mediaThumbnails, NullLogger<PlaylistThumbnailService>.Instance)
        {
            CacheFolderPath = fixture.DirectoryPath
        };

        byte[]? result = await service.BuildAndCacheThumbnailAsync(
            Guid.NewGuid().ToString(),
            new[] { albumATrack1, albumATrack1Dup, albumATrack2, albumATrack3, video1, video1Dup });

        await Assert.That(result).IsNotNull();
        await Assert.That(probedSources.Count).IsEqualTo(3);
        await Assert.That(probedSources[0]).IsSameReferenceAs(albumATrack1.Source);
        await Assert.That(probedSources[1]).IsSameReferenceAs(albumATrack2.Source);
        await Assert.That(probedSources[2]).IsSameReferenceAs(video1.Source);

        var decoded = await DecodeBgraPixelsAsync(result!);
        await AssertColorNearAsync(GetRgbPixel(decoded.Pixels, decoded.Width, 100, 200), red);
        await AssertColorNearAsync(GetRgbPixel(decoded.Pixels, decoded.Width, 300, 200), blue);
    }

    [Test]
    public async Task BuildAndCacheThumbnailAsync_RespectsMaxScanCount()
    {
        using var fixture = new TestDirectoryFixture();
        var items = Enumerable.Range(0, 35)
            .Select(i => CreateItem($"https://example.com/v{i}.mp4"))
            .ToArray();

        int probeCount = 0;
        var mediaThumbnails = new FakeMediaThumbnailService(async (source, _, _) =>
        {
            probeCount++;
            if (ReferenceEquals(source, items[32].Source))
                return await CreateSolidPngStreamAsync(200, 200, (220, 40, 40));

            return null;
        });

        var service = new PlaylistThumbnailService(mediaThumbnails, NullLogger<PlaylistThumbnailService>.Instance)
        {
            CacheFolderPath = fixture.DirectoryPath
        };

        string playlistId = Guid.NewGuid().ToString();
        byte[]? result = await service.BuildAndCacheThumbnailAsync(playlistId, items);

        await Assert.That(result).IsNull();
        await Assert.That(probeCount).IsEqualTo(32);
        await Assert.That(await service.ReadThumbnailAsync(playlistId)).IsNull();
    }

    [Test]
    public async Task CacheOperations_WriteReadOverwriteEmptyAndDelete_WorkAtomicallyWithoutTempLeak()
    {
        using var fixture = new TestDirectoryFixture();
        (byte R, byte G, byte B) red = (220, 40, 40);
        (byte R, byte G, byte B) blue = (40, 80, 220);

        var redVideo = CreateItem("https://example.com/red.mp4");
        var blueVideo = CreateItem("https://example.com/blue.mp4");

        var mediaThumbnails = new FakeMediaThumbnailService(async (source, _, _) =>
            ReferenceEquals(source, redVideo.Source)
                ? await CreateSolidPngStreamAsync(200, 200, red)
                : await CreateSolidPngStreamAsync(200, 200, blue));

        var service = new PlaylistThumbnailService(mediaThumbnails, NullLogger<PlaylistThumbnailService>.Instance)
        {
            CacheFolderPath = fixture.DirectoryPath
        };

        string playlistId = Guid.NewGuid().ToString();

        await Assert.That(await service.ReadThumbnailAsync(playlistId)).IsNull();

        byte[]? firstBytes = await service.BuildAndCacheThumbnailAsync(playlistId, new[] { redVideo });
        await Assert.That(firstBytes).IsNotNull();
        await Assert.That(Directory.GetFiles(fixture.DirectoryPath, "*.tmp").Length).IsEqualTo(0);

        byte[]? readFirst = await service.ReadThumbnailAsync(playlistId);
        await Assert.That(readFirst).IsNotNull();
        await Assert.That(readFirst!.SequenceEqual(firstBytes!)).IsTrue();

        byte[]? updatedBytes = await service.BuildAndCacheThumbnailAsync(playlistId, new[] { blueVideo });
        await Assert.That(updatedBytes).IsNotNull();
        await Assert.That(Directory.GetFiles(fixture.DirectoryPath, "*.tmp").Length).IsEqualTo(0);

        byte[]? readUpdated = await service.ReadThumbnailAsync(playlistId);
        await Assert.That(readUpdated).IsNotNull();
        await Assert.That(readUpdated!.SequenceEqual(updatedBytes!)).IsTrue();

        var decodedUpdated = await DecodeBgraPixelsAsync(readUpdated!);
        await AssertColorNearAsync(GetRgbPixel(decodedUpdated.Pixels, decodedUpdated.Width, 200, 200), blue);

        byte[]? emptyResult = await service.BuildAndCacheThumbnailAsync(playlistId, Array.Empty<PlaylistThumbnailItem>());
        await Assert.That(emptyResult).IsNull();
        await Assert.That(await service.ReadThumbnailAsync(playlistId)).IsNull();

        await service.DeleteThumbnailAsync(playlistId);
    }
}

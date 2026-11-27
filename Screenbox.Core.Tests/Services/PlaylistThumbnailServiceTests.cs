using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.Services;

public class PlaylistThumbnailServiceTests
{
    private static MediaViewModel CreateMusicMedia(string location, string album, string albumArtist = "Artist")
    {
        var info = new MediaInfo(MediaPlaybackType.Music);
        info.MusicProperties.Album = album;
        info.MusicProperties.AlbumArtist = albumArtist;

        var media = new MediaViewModel(new UriMediaSource(new Uri(location)))
        {
            Name = Path.GetFileNameWithoutExtension(location),
            MediaInfo = info
        };

        return media;
    }

    private static MediaViewModel CreateVideoMedia(string location) =>
        new(new UriMediaSource(new Uri(location)))
        {
            Name = Path.GetFileNameWithoutExtension(location),
            MediaInfo = new MediaInfo(MediaPlaybackType.Video)
        };

    private static byte[] CreateSolidPixels(int width, int height, byte b, byte g, byte r, byte a = 255)
    {
        byte[] buffer = new byte[width * height * 4];
        for (int i = 0; i < buffer.Length; i += 4)
        {
            buffer[i] = b;
            buffer[i + 1] = g;
            buffer[i + 2] = r;
            buffer[i + 3] = a;
        }

        return buffer;
    }

    private static (byte B, byte G, byte R, byte A) GetPixel(byte[] canvas, int canvasSize, int x, int y)
    {
        int offset = (y * canvasSize + x) * 4;
        return (canvas[offset], canvas[offset + 1], canvas[offset + 2], canvas[offset + 3]);
    }

    [Test]
    public async Task GetCollageLayout_CoversFullCanvasForOneToFourArtworks()
    {
        const int canvasSize = 400;

        var layout0 = PlaylistThumbnailService.GetCollageLayout(0, canvasSize);
        await Assert.That(layout0.Length).IsEqualTo(0);

        var layout1 = PlaylistThumbnailService.GetCollageLayout(1, canvasSize);
        await Assert.That(layout1.Length).IsEqualTo(1);
        await Assert.That(layout1[0]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(0, 0, 400, 400));

        var layout2 = PlaylistThumbnailService.GetCollageLayout(2, canvasSize);
        await Assert.That(layout2.Length).IsEqualTo(2);
        await Assert.That(layout2[0]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(0, 0, 200, 400));
        await Assert.That(layout2[1]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(200, 0, 200, 400));

        var layout3 = PlaylistThumbnailService.GetCollageLayout(3, canvasSize);
        await Assert.That(layout3.Length).IsEqualTo(3);
        await Assert.That(layout3[0]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(0, 0, 200, 400));
        await Assert.That(layout3[1]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(200, 0, 200, 200));
        await Assert.That(layout3[2]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(200, 200, 200, 200));

        var layout4 = PlaylistThumbnailService.GetCollageLayout(4, canvasSize);
        await Assert.That(layout4.Length).IsEqualTo(4);
        await Assert.That(layout4[0]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(0, 0, 200, 200));
        await Assert.That(layout4[1]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(200, 0, 200, 200));
        await Assert.That(layout4[2]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(0, 200, 200, 200));
        await Assert.That(layout4[3]).IsEqualTo(new PlaylistThumbnailService.CollageSlot(200, 200, 200, 200));
    }

    [Test]
    public async Task CompositeCollagePixels_ThreeArtworkSplit_FillsLeftHalfAndTwoRightSquares()
    {
        const int canvasSize = 8;
        var layout = PlaylistThumbnailService.GetCollageLayout(3, canvasSize);
        var cells = new List<(PlaylistThumbnailService.CollageSlot, byte[])>
        {
            (layout[0], CreateSolidPixels(layout[0].Width, layout[0].Height, b: 10, g: 20, r: 30)),
            (layout[1], CreateSolidPixels(layout[1].Width, layout[1].Height, b: 40, g: 50, r: 60)),
            (layout[2], CreateSolidPixels(layout[2].Width, layout[2].Height, b: 70, g: 80, r: 90))
        };

        byte[] canvas = PlaylistThumbnailService.CompositeCollagePixels(canvasSize, cells);

        for (int y = 0; y < canvasSize; y++)
        {
            for (int x = 0; x < canvasSize; x++)
            {
                var pixel = GetPixel(canvas, canvasSize, x, y);
                if (x < 4)
                {
                    await Assert.That(pixel).IsEqualTo(((byte)10, (byte)20, (byte)30, (byte)255));
                }
                else if (y < 4)
                {
                    await Assert.That(pixel).IsEqualTo(((byte)40, (byte)50, (byte)60, (byte)255));
                }
                else
                {
                    await Assert.That(pixel).IsEqualTo(((byte)70, (byte)80, (byte)90, (byte)255));
                }
            }
        }
    }

    [Test]
    public async Task CompositeCollagePixels_TwoAndFourArtworkLayouts_FillExpectedRegions()
    {
        const int canvasSize = 8;

        var layout2 = PlaylistThumbnailService.GetCollageLayout(2, canvasSize);
        byte[] canvas2 = PlaylistThumbnailService.CompositeCollagePixels(canvasSize, new (PlaylistThumbnailService.CollageSlot, byte[])[]
        {
            (layout2[0], CreateSolidPixels(4, 8, b: 1, g: 2, r: 3)),
            (layout2[1], CreateSolidPixels(4, 8, b: 4, g: 5, r: 6))
        });

        await Assert.That(GetPixel(canvas2, canvasSize, 0, 0)).IsEqualTo(((byte)1, (byte)2, (byte)3, (byte)255));
        await Assert.That(GetPixel(canvas2, canvasSize, 3, 7)).IsEqualTo(((byte)1, (byte)2, (byte)3, (byte)255));
        await Assert.That(GetPixel(canvas2, canvasSize, 4, 0)).IsEqualTo(((byte)4, (byte)5, (byte)6, (byte)255));
        await Assert.That(GetPixel(canvas2, canvasSize, 7, 7)).IsEqualTo(((byte)4, (byte)5, (byte)6, (byte)255));

        var layout4 = PlaylistThumbnailService.GetCollageLayout(4, canvasSize);
        byte[] canvas4 = PlaylistThumbnailService.CompositeCollagePixels(canvasSize, new (PlaylistThumbnailService.CollageSlot, byte[])[]
        {
            (layout4[0], CreateSolidPixels(4, 4, b: 11, g: 12, r: 13)),
            (layout4[1], CreateSolidPixels(4, 4, b: 21, g: 22, r: 23)),
            (layout4[2], CreateSolidPixels(4, 4, b: 31, g: 32, r: 33)),
            (layout4[3], CreateSolidPixels(4, 4, b: 41, g: 42, r: 43))
        });

        await Assert.That(GetPixel(canvas4, canvasSize, 0, 0)).IsEqualTo(((byte)11, (byte)12, (byte)13, (byte)255));
        await Assert.That(GetPixel(canvas4, canvasSize, 4, 0)).IsEqualTo(((byte)21, (byte)22, (byte)23, (byte)255));
        await Assert.That(GetPixel(canvas4, canvasSize, 0, 4)).IsEqualTo(((byte)31, (byte)32, (byte)33, (byte)255));
        await Assert.That(GetPixel(canvas4, canvasSize, 7, 7)).IsEqualTo(((byte)41, (byte)42, (byte)43, (byte)255));
    }

    private readonly record struct TestArtwork(MediaViewModel Item);

    [Test]
    public async Task CollectDistinctArtworksAsync_DeduplicatesSameAlbumAndDuplicateLocations()
    {
        var track1 = CreateMusicMedia("https://example.com/a1.mp3", "Album A", "Artist 1");
        var track2 = CreateMusicMedia("https://example.com/a2.mp3", "Album A", "Artist 1");
        var track3 = CreateMusicMedia("https://example.com/b1.mp3", "Album B", "Artist 2");
        var video1 = CreateVideoMedia("https://example.com/v1.mp4");
        var video1Duplicate = CreateVideoMedia("https://example.com/v1.mp4");
        var video2 = CreateVideoMedia("https://example.com/v2.mp4");

        var collected = new List<TestArtwork>();
        await PlaylistThumbnailService.CollectDistinctArtworksAsync<TestArtwork>(
            new[] { track1, track2, track3, video1, video1Duplicate, video2 },
            collected,
            (item, _) => Task.FromResult<TestArtwork?>(new TestArtwork(item)));

        await Assert.That(collected.Count).IsEqualTo(4);
        await Assert.That(collected[0].Item).IsSameReferenceAs(track1);
        await Assert.That(collected[1].Item).IsSameReferenceAs(track3);
        await Assert.That(collected[2].Item).IsSameReferenceAs(video1);
        await Assert.That(collected[3].Item).IsSameReferenceAs(video2);
    }

    [Test]
    public async Task CollectDistinctArtworksAsync_RespectsMaxScanCount()
    {
        var items = Enumerable.Range(0, 10)
            .Select(i => CreateVideoMedia($"https://example.com/v{i}.mp4"))
            .ToArray();

        var collected = new List<TestArtwork>();
        await PlaylistThumbnailService.CollectDistinctArtworksAsync<TestArtwork>(
            items,
            collected,
            (item, _) => Task.FromResult<TestArtwork?>(new TestArtwork(item)),
            maxArtworkCount: 10,
            maxScanCount: 5);

        await Assert.That(collected.Count).IsEqualTo(5);
    }

    [Test]
    public async Task CollectDistinctArtworksAsync_UnreadableFirstAlbumTrack_FallsBackToReadableLaterTrackAndSkipsFailedDuplicateLocation()
    {
        var albumATrack1 = CreateMusicMedia("https://example.com/a1.mp3", "Album A", "Artist 1");
        var albumATrack1Dup = CreateMusicMedia("https://example.com/a1.mp3", "Album A", "Artist 1");
        var albumATrack2 = CreateMusicMedia("https://example.com/a2.mp3", "Album A", "Artist 1");
        var albumATrack3 = CreateMusicMedia("https://example.com/a3.mp3", "Album A", "Artist 1");
        var albumBTrack1 = CreateMusicMedia("https://example.com/b1.mp3", "Album B", "Artist 2");

        var probedItems = new List<MediaViewModel>();
        var collected = new List<TestArtwork>();

        await PlaylistThumbnailService.CollectDistinctArtworksAsync<TestArtwork>(
            new[] { albumATrack1, albumATrack1Dup, albumATrack2, albumATrack3, albumBTrack1 },
            collected,
            (item, _) =>
            {
                probedItems.Add(item);
                if (ReferenceEquals(item, albumATrack1) || ReferenceEquals(item, albumATrack1Dup))
                {
                    return Task.FromResult<TestArtwork?>(null);
                }

                return Task.FromResult<TestArtwork?>(new TestArtwork(item));
            });

        await Assert.That(collected.Count).IsEqualTo(2);
        await Assert.That(collected[0].Item).IsSameReferenceAs(albumATrack2);
        await Assert.That(collected[1].Item).IsSameReferenceAs(albumBTrack1);
        await Assert.That(probedItems.Count).IsEqualTo(3);
        await Assert.That(probedItems[0]).IsSameReferenceAs(albumATrack1);
        await Assert.That(probedItems[1]).IsSameReferenceAs(albumATrack2);
        await Assert.That(probedItems[2]).IsSameReferenceAs(albumBTrack1);
    }

    [Test]
    public async Task CacheOperations_WriteReadOverwriteAndDelete_WorkAtomicallyWithoutTempLeak()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "ScreenboxThumbnailTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new PlaylistThumbnailService(NullLogger<PlaylistThumbnailService>.Instance)
            {
                CacheFolderPath = tempDir
            };

            string playlistId = Guid.NewGuid().ToString();

            byte[]? initial = await service.ReadThumbnailAsync(playlistId);
            await Assert.That(initial).IsNull();

            byte[] firstBytes = [1, 2, 3, 4, 5];
            await service.WriteThumbnailAtomicallyAsync(playlistId, firstBytes, CancellationToken.None);

            byte[]? readFirst = await service.ReadThumbnailAsync(playlistId);
            await Assert.That(readFirst).IsNotNull();
            await Assert.That(readFirst!.SequenceEqual(firstBytes)).IsTrue();
            await Assert.That(Directory.GetFiles(tempDir, "*.tmp").Length).IsEqualTo(0);

            byte[] updatedBytes = [9, 8, 7];
            await service.WriteThumbnailAtomicallyAsync(playlistId, updatedBytes, CancellationToken.None);

            byte[]? readUpdated = await service.ReadThumbnailAsync(playlistId);
            await Assert.That(readUpdated).IsNotNull();
            await Assert.That(readUpdated!.SequenceEqual(updatedBytes)).IsTrue();
            await Assert.That(Directory.GetFiles(tempDir, "*.tmp").Length).IsEqualTo(0);

            byte[]? emptyResult = await service.BuildAndCacheThumbnailAsync(playlistId, Array.Empty<MediaViewModel>());
            await Assert.That(emptyResult).IsNull();
            await Assert.That(await service.ReadThumbnailAsync(playlistId)).IsNull();

            await service.DeleteThumbnailAsync(playlistId);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}

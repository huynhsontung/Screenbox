using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Windows.Storage;

namespace Screenbox.Core.Tests.Services;

public class MediaProbeServiceTests
{
    [Test]
    public async Task ProbeMediaAsync_WithNullMedia_ShouldThrowArgumentNullException()
    {
        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);
        await Assert.That(async () => await service.ProbeMediaAsync((Media)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullPlaybackItem_ShouldThrowArgumentNullException()
    {
        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);
        await Assert.That(async () => await service.ProbeMediaAsync((PlaybackItem)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullStorageFile_ShouldThrowArgumentNullException()
    {
        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);
        await Assert.That(async () => await service.ProbeMediaAsync((StorageFile)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullUri_ShouldThrowArgumentNullException()
    {
        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);
        await Assert.That(async () => await service.ProbeMediaAsync((Uri)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullObjectSource_ShouldReturnNull()
    {
        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);
        var result = await service.ProbeMediaAsync((object)null!);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_WhenMediaPlayerNotInitialized_ShouldReturnNullGracefully()
    {
        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);
        var result = await service.ProbeMediaAsync(new Uri("https://example.com/stream.m3u8"));
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_PolymorphicObjectDispatch_RoutesToUriOverload()
    {
        var testService = new TestableMediaProbeService();
        var uri = new Uri("https://example.com/test.mp4");

        var result = await testService.ProbeMediaAsync((object)uri);

        await Assert.That(testService.UriOverloadCalled).IsTrue();
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ProbeMediaAsync_PolymorphicObjectDispatch_RoutesToStorageFileOverload()
    {
        using var fixture = new TestDirectoryFixture();
        string filePath = Path.Combine(fixture.DirectoryPath, "sample.mp4");
        await File.WriteAllBytesAsync(filePath, [0, 1, 2, 3]);
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

        var testService = new TestableMediaProbeService();
        var result = await testService.ProbeMediaAsync((object)file);

        await Assert.That(testService.StorageFileOverloadCalled).IsTrue();
        await Assert.That(result).IsNotNull();
    }

    private sealed class TestableMediaProbeService : MediaProbeService
    {
        public bool UriOverloadCalled { get; private set; }
        public bool StorageFileOverloadCalled { get; private set; }

        public TestableMediaProbeService()
            : base(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance)
        {
        }

        public override Task<MediaInfo?> ProbeMediaAsync(
            Uri uri,
            string[]? options = null,
            CancellationToken cancellationToken = default)
        {
            UriOverloadCalled = true;
            return Task.FromResult<MediaInfo?>(new MediaInfo(MediaPlaybackType.Video));
        }

        public override Task<MediaInfo?> ProbeMediaAsync(
            StorageFile file,
            string[]? options = null,
            CancellationToken cancellationToken = default)
        {
            StorageFileOverloadCalled = true;
            return Task.FromResult<MediaInfo?>(new MediaInfo(MediaPlaybackType.Video));
        }
    }
}

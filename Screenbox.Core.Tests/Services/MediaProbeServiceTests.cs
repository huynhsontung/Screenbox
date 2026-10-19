using System;
using System.IO;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
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
    public async Task ProbeMediaAsync_WhenMediaPlayerNotInitialized_ShouldReturnNull()
    {
        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);
        var uri = new Uri("https://example.com/stream.m3u8");

        var typedResult = await service.ProbeMediaAsync(uri);
        var polymorphicResult = await service.ProbeMediaAsync((object)uri);

        await Assert.That(typedResult).IsNull();
        await Assert.That(polymorphicResult).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_WithStorageFile_WhenMediaPlayerNotInitialized_ShouldReturnNull()
    {
        using var fixture = new TestDirectoryFixture();
        string filePath = Path.Combine(fixture.DirectoryPath, "sample.mp4");
        await File.WriteAllBytesAsync(filePath, [0, 1, 2, 3]);
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

        var service = new MediaProbeService(new PlayerContext(), new TestPlayerService(), NullLogger<MediaProbeService>.Instance);

        var typedResult = await service.ProbeMediaAsync(file);
        var polymorphicResult = await service.ProbeMediaAsync((object)file);

        await Assert.That(typedResult).IsNull();
        await Assert.That(polymorphicResult).IsNull();
    }
}

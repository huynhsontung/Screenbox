using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Windows.Storage;

namespace Screenbox.Core.Tests.Services;

public class StorageMediaProbeServiceTests
{
    [Test]
    public async Task ProbeMediaAsync_WithNullStorageFile_ShouldThrowArgumentNullException()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        await Assert.That(async () => await service.ProbeMediaAsync((StorageFile)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullSource_ShouldReturnNull()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var result = await service.ProbeMediaAsync((object)null!);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_WithUnsupportedObject_ShouldReturnNull()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var result = await service.ProbeMediaAsync(12345);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNonExistentPathString_ShouldReturnNull()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var result = await service.ProbeMediaAsync(@"C:\nonexistent_path_xyz\test.mp4");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_StorageFileOverload_ExtractsVideoMediaInfo()
    {
        using var fixture = new TestDirectoryFixture();
        string filePath = Path.Combine(fixture.DirectoryPath, "sample.mp4");
        await File.WriteAllBytesAsync(filePath, [0, 1, 2, 3]);
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var info = await service.ProbeMediaAsync(file);

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.MediaType).IsEqualTo(MediaPlaybackType.Video);
    }

    [Test]
    public async Task ProbeMediaAsync_StorageFileOverload_ExtractsMusicMediaInfo()
    {
        using var fixture = new TestDirectoryFixture();
        string filePath = Path.Combine(fixture.DirectoryPath, "sample.mp3");
        await File.WriteAllBytesAsync(filePath, [0, 1, 2, 3]);
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var info = await service.ProbeMediaAsync(file);

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task ProbeMediaAsync_PolymorphicStorageFile_ExtractsBasicMediaInfo()
    {
        using var fixture = new TestDirectoryFixture();
        string filePath = Path.Combine(fixture.DirectoryPath, "sample.mp3");
        await File.WriteAllBytesAsync(filePath, [0, 1, 2, 3]);
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var info = await service.ProbeMediaAsync((object)file);

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task ProbeMediaAsync_PolymorphicPathString_ExtractsBasicMediaInfo()
    {
        using var fixture = new TestDirectoryFixture();
        string filePath = Path.Combine(fixture.DirectoryPath, "sample.mp3");
        await File.WriteAllBytesAsync(filePath, [0, 1, 2, 3]);

        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var info = await service.ProbeMediaAsync((object)filePath);

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task ProbeMediaAsync_PolymorphicFileUri_ExtractsBasicMediaInfo()
    {
        using var fixture = new TestDirectoryFixture();
        string filePath = Path.Combine(fixture.DirectoryPath, "sample.mp4");
        await File.WriteAllBytesAsync(filePath, [0, 1, 2, 3]);
        var uri = new Uri(filePath);

        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var info = await service.ProbeMediaAsync((object)uri);

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.MediaType).IsEqualTo(MediaPlaybackType.Video);
    }
}

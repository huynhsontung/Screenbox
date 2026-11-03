using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;

namespace Screenbox.Core.Tests.Services;

public partial class StorageMetadataServiceTests
{
    [Test]
    public async Task GetMediaInfoAsync_WithNullArguments_ShouldThrowArgumentNullException()
    {
        var service = new StorageMetadataService(NullLogger<StorageMetadataService>.Instance);

        await Assert.That(async () => await service.GetMediaInfoAsync((StorageFile)null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await service.GetMediaInfoAsync((IFileMediaSource)null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await service.GetMediaInfoAsync((IMediaSource)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetMediaInfoAsync_WithNonFileUriMediaSource_ShouldReturnNull()
    {
        var service = new StorageMetadataService(NullLogger<StorageMetadataService>.Instance);
        var uriSource = new UriMediaSource(new Uri("https://example.com/stream.m3u8"));

        var result = await service.GetMediaInfoAsync(uriSource);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetMediaInfoAsync_WithNonExistentFileMediaSource_ShouldReturnNull()
    {
        var service = new StorageMetadataService(NullLogger<StorageMetadataService>.Instance);
        var fileSource = new FileMediaSource(@"C:\nonexistent_folder_abc123\video.mp4");

        var result = await service.GetMediaInfoAsync(fileSource);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetMediaInfoAsync_WithExistingStorageFile_ShouldReturnMediaInfoWithSizeAndModifiedDate()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"screenbox_metadata_test_{Guid.NewGuid():N}.mp4");
        byte[] content = new byte[1024];
        await File.WriteAllBytesAsync(tempPath, content);

        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(tempPath);
            var service = new StorageMetadataService(NullLogger<StorageMetadataService>.Instance);

            MediaInfo? info = await service.GetMediaInfoAsync(file);

            await Assert.That(info).IsNotNull();
            await Assert.That(info!.MediaType).IsEqualTo(MediaPlaybackType.Video);
            await Assert.That(info.Size).IsEqualTo(1024UL);
            await Assert.That(info.DateModified).IsNotEqualTo(default(DateTimeOffset));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}

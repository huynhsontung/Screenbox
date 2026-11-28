using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Services;
using Windows.Storage.Streams;

namespace Screenbox.Core.Tests.Services;

public class MediaThumbnailServiceTests
{
    private static MediaThumbnailService CreateService() =>
        new(new FilesService(NullLogger<FilesService>.Instance), NullLogger<MediaThumbnailService>.Instance);

    [Test]
    public async Task GetThumbnailStreamAsync_UriMediaSource_ReturnsNull()
    {
        MediaThumbnailService service = CreateService();
        UriMediaSource source = new(new Uri("https://example.com/song.mp3"));

        IRandomAccessStream? stream = await service.GetThumbnailStreamAsync(source);

        await Assert.That(stream).IsNull();
    }

    [Test]
    public async Task GetThumbnailStreamAsync_CanceledToken_ReturnsNull()
    {
        MediaThumbnailService service = CreateService();
        FileMediaSource source = new(@"C:\nonexistent\song.mp3");
        using CancellationTokenSource cts = new();
        cts.Cancel();

        IRandomAccessStream? stream = await service.GetThumbnailStreamAsync(source, cancellationToken: cts.Token);

        await Assert.That(stream).IsNull();
    }

    [Test]
    public async Task GetThumbnailStreamAsync_MissingFileSource_ReturnsNull()
    {
        MediaThumbnailService service = CreateService();
        FileMediaSource source = new(@"C:\nonexistent\missing_file_for_thumbnail_test.mp3");

        IRandomAccessStream? stream = await service.GetThumbnailStreamAsync(source);

        await Assert.That(stream).IsNull();
    }
}

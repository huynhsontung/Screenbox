using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Media.Core;
using Windows.Storage;
using AudioTrack = Screenbox.Core.Playback.AudioTrack;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;
using VideoTrack = Screenbox.Core.Playback.VideoTrack;

namespace Screenbox.Core.Tests.Services;

public partial class StorageMediaProbeServiceTests
{
    private sealed partial class DummyPlaybackItem : IPlaybackItem
    {
        public IMediaSource Source { get; init; } = null!;
        public TimeSpan? Duration => null;
        public ISingleSelectMediaTrackList<AudioTrack> AudioTracks => throw new NotImplementedException();
        public ISingleSelectMediaTrackList<VideoTrack> VideoTracks => throw new NotImplementedException();
        public ISubtitleTrackList SubtitleTracks => throw new NotImplementedException();
        public IReadOnlyList<ChapterCue> Chapters => Array.Empty<ChapterCue>();
        public Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExpandedMediaItem>>(Array.Empty<ExpandedMediaItem>());
        public void Dispose() { }
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullArguments_ShouldThrowArgumentNullException()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);

        await Assert.That(async () => await service.ProbeMediaAsync((StorageFile)null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await service.ProbeMediaAsync((IFileMediaSource)null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await service.ProbeMediaAsync((IMediaSource)null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await service.ProbeMediaAsync((IPlaybackItem)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNonFileUriMediaSource_ShouldReturnNull()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var uriSource = new UriMediaSource(new Uri("https://example.com/stream.m3u8"));

        var result = await service.ProbeMediaAsync(uriSource);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_WithNonExistentFileMediaSource_ShouldReturnNull()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var fileSource = new FileMediaSource(@"C:\nonexistent_folder_abc123\video.mp4");

        var result = await service.ProbeMediaAsync(fileSource);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_WithExistingStorageFile_ShouldReturnMediaInfoWithSizeAndModifiedDate()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"screenbox_probe_test_{Guid.NewGuid():N}.mp4");
        byte[] content = new byte[1024];
        await File.WriteAllBytesAsync(tempPath, content);

        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(tempPath);
            var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);

            MediaInfo? info = await service.ProbeMediaAsync(file);

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

    [Test]
    public async Task ProbeMediaAsync_WithPlaybackItem_DelegatesToUnderlyingSource()
    {
        var service = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        var uriSource = new UriMediaSource(new Uri("https://example.com/stream.m3u8"));
        var item = new DummyPlaybackItem { Source = uriSource };

        var result = await service.ProbeMediaAsync(item);

        await Assert.That(result).IsNull();
    }
}

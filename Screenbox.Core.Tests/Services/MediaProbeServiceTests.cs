using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Media.Core;
using AudioTrack = Screenbox.Core.Playback.AudioTrack;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;
using VideoTrack = Screenbox.Core.Playback.VideoTrack;

namespace Screenbox.Core.Tests.Services;

public partial class MediaProbeServiceTests
{
    private sealed partial class FakePlaybackItem : IPlaybackItem
    {
        private readonly MediaInfo? _result;
        private readonly Exception? _exception;

        public IMediaSource Source { get; }
        public bool IsDisposed { get; private set; }
        public int ProbeCount { get; private set; }
        public TimeSpan? Duration => null;
        public ISingleSelectMediaTrackList<AudioTrack> AudioTracks => throw new NotImplementedException();
        public ISingleSelectMediaTrackList<VideoTrack> VideoTracks => throw new NotImplementedException();
        public ISubtitleTrackList SubtitleTracks => throw new NotImplementedException();
        public IReadOnlyList<ChapterCue> Chapters => Array.Empty<ChapterCue>();

        public FakePlaybackItem(IMediaSource source, MediaInfo? result = null, Exception? exception = null)
        {
            Source = source;
            _result = result;
            _exception = exception;
        }

        public Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<MediaInfo?> ProbeMediaInfoAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProbeCount++;
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_result);
        }

        public Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExpandedMediaItem>>(Array.Empty<ExpandedMediaItem>());

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class FakePlayerService : IPlayerService
    {
        private readonly MediaInfo? _result;

        public int CreateCount { get; private set; }
        public FakePlaybackItem? LastCreatedItem { get; private set; }
        public string[]? LastOptions { get; private set; }

        public FakePlayerService(MediaInfo? result = null)
        {
            _result = result;
        }

        public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

        public IPlaybackItem CreatePlaybackItem(IMediaPlayer player, IMediaSource source, params string[] options)
        {
            CreateCount++;
            LastOptions = options;
            LastCreatedItem = new FakePlaybackItem(source, _result);
            return LastCreatedItem;
        }

        public void DisposePlayer(IMediaPlayer player) { }
    }

    private sealed partial class FakeBufferManager : IPlayQueueBufferManager
    {
        private readonly Dictionary<IMediaSource, IPlaybackItem> _items = new();

        public IReadOnlyList<MediaViewModel> BufferedItems => Array.Empty<MediaViewModel>();

        public void AddBufferedItem(IMediaSource source, IPlaybackItem item) => _items[source] = item;

        public bool TryGetPlaybackItem(IMediaSource source, [NotNullWhen(true)] out IPlaybackItem? item) =>
            _items.TryGetValue(source, out item);

        public IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, MediaViewModel vm) => null;

        public void CleanPlaybackItem(MediaViewModel vm) { }

        public void Clear() => _items.Clear();

        public void UpdateBuffer(IMediaPlayer? player, Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode) { }

        public void Dispose() { }
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullArguments_ShouldThrowArgumentNullException()
    {
        var service = new MediaProbeService(
            new PlayerContext(),
            new FakePlayerService(),
            new FakeBufferManager(),
            NullLogger<MediaProbeService>.Instance);

        await Assert.That(async () => await service.ProbeMediaAsync((IMediaSource)null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await service.ProbeMediaAsync((IPlaybackItem)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WhenActivePlaybackItemMatchesSource_ReusesActiveItemWithoutCreatingNew()
    {
        var source = new UriMediaSource(new Uri("https://example.com/live.m3u8"));
        var expectedInfo = new MediaInfo(MediaPlaybackType.Video, title: "Active Stream");
        var activeItem = new FakePlaybackItem(source, expectedInfo);
        var player = new TestMediaPlayer { PlaybackItem = activeItem };
        var playerContext = new PlayerContext { MediaPlayer = player };
        var playerService = new FakePlayerService();
        var bufferManager = new FakeBufferManager();
        var service = new MediaProbeService(playerContext, playerService, bufferManager, NullLogger<MediaProbeService>.Instance);

        var result = await service.ProbeMediaAsync(source);

        await Assert.That(result).IsSameReferenceAs(expectedInfo);
        await Assert.That(activeItem.ProbeCount).IsEqualTo(1);
        await Assert.That(activeItem.IsDisposed).IsFalse();
        await Assert.That(playerService.CreateCount).IsEqualTo(0);
    }

    [Test]
    public async Task ProbeMediaAsync_WhenSourceIsInBufferManager_ReusesBufferedItemWithoutCreatingNew()
    {
        var source = new FileMediaSource(@"C:\Videos\buffered.mkv");
        var expectedInfo = new MediaInfo(MediaPlaybackType.Video, title: "Buffered Video");
        var bufferedItem = new FakePlaybackItem(source, expectedInfo);
        var playerContext = new PlayerContext { MediaPlayer = new TestMediaPlayer() };
        var playerService = new FakePlayerService();
        var bufferManager = new FakeBufferManager();
        bufferManager.AddBufferedItem(source, bufferedItem);
        var service = new MediaProbeService(playerContext, playerService, bufferManager, NullLogger<MediaProbeService>.Instance);

        var result = await service.ProbeMediaAsync(source);

        await Assert.That(result).IsSameReferenceAs(expectedInfo);
        await Assert.That(bufferedItem.ProbeCount).IsEqualTo(1);
        await Assert.That(bufferedItem.IsDisposed).IsFalse();
        await Assert.That(playerService.CreateCount).IsEqualTo(0);
    }

    [Test]
    public async Task ProbeMediaAsync_WhenSourceNotActiveOrBuffered_CreatesAndDisposesTemporaryPlaybackItem()
    {
        var source = new FileMediaSource(@"C:\Music\track.ogg");
        var expectedInfo = new MediaInfo(MediaPlaybackType.Music, title: "Probed Track");
        var playerContext = new PlayerContext { MediaPlayer = new TestMediaPlayer() };
        var playerService = new FakePlayerService(expectedInfo);
        var bufferManager = new FakeBufferManager();
        var service = new MediaProbeService(playerContext, playerService, bufferManager, NullLogger<MediaProbeService>.Instance);

        var result = await service.ProbeMediaAsync(source, new[] { ":network-caching=1000" });

        await Assert.That(result).IsSameReferenceAs(expectedInfo);
        await Assert.That(playerService.CreateCount).IsEqualTo(1);
        await Assert.That(playerService.LastOptions).IsNotNull();
        await Assert.That(playerService.LastOptions![0]).IsEqualTo(":network-caching=1000");
        await Assert.That(playerService.LastCreatedItem).IsNotNull();
        await Assert.That(playerService.LastCreatedItem!.ProbeCount).IsEqualTo(1);
        await Assert.That(playerService.LastCreatedItem!.IsDisposed).IsTrue();
    }

    [Test]
    public async Task ProbeMediaAsync_WhenNoMediaPlayer_ReturnsNull()
    {
        var source = new UriMediaSource(new Uri("https://example.com/video.mp4"));
        var service = new MediaProbeService(
            new PlayerContext(),
            new FakePlayerService(),
            new FakeBufferManager(),
            NullLogger<MediaProbeService>.Instance);

        var result = await service.ProbeMediaAsync(source);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProbeMediaAsync_WhenCanceled_ReturnsNull()
    {
        var source = new UriMediaSource(new Uri("https://example.com/video.mp4"));
        var item = new FakePlaybackItem(source, new MediaInfo(MediaPlaybackType.Video));
        var service = new MediaProbeService(
            new PlayerContext(),
            new FakePlayerService(),
            new FakeBufferManager(),
            NullLogger<MediaProbeService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await service.ProbeMediaAsync(item, cts.Token);

        await Assert.That(result).IsNull();
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
using VideoTrack = Screenbox.Core.Playback.VideoTrack;

namespace Screenbox.Core.Tests.Playback;

public partial class PlayQueueBufferManagerTests
{
    private sealed partial class DummyPlaybackItem : IPlaybackItem
    {
        public bool IsDisposed { get; private set; }
        public IMediaSource Source { get; init; } = null!;
        public TimeSpan? Duration => TimeSpan.Zero;
        public ISingleSelectMediaTrackList<AudioTrack> AudioTracks => throw new NotImplementedException();
        public ISingleSelectMediaTrackList<VideoTrack> VideoTracks => throw new NotImplementedException();
        public ISubtitleTrackList SubtitleTracks => throw new NotImplementedException();
        public IReadOnlyList<ChapterCue> Chapters => Array.Empty<ChapterCue>();

        public Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExpandedMediaItem>>(Array.Empty<ExpandedMediaItem>());

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class MockPlayerService : IPlayerService
    {
        public int CreateCount { get; private set; }
        public List<DummyPlaybackItem> CreatedItems { get; } = new();

        public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

        public IPlaybackItem CreatePlaybackItem(IMediaPlayer player, IMediaSource source, params string[] options)
        {
            CreateCount++;
            var item = new DummyPlaybackItem { Source = source };
            CreatedItems.Add(item);
            return item;
        }

        public void DisposePlayer(IMediaPlayer player) { }
    }

    private static MediaViewModel CreateMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private static QueueItemViewModel CreateQueueItem(string name) =>
        new(CreateMedia(name));

    [Test]
    public async Task GetOrCreatePlaybackItem_CachesAndReturnsSameItem()
    {
        var playerService = new MockPlayerService();
        var queueService = new QueueService(null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, queueService);
        var player = new TestMediaPlayer();
        var queueItem = CreateQueueItem("test1");

        var item1 = bufferManager.GetOrCreatePlaybackItem(player, queueItem);
        var item2 = bufferManager.GetOrCreatePlaybackItem(player, queueItem);

        await Assert.That(item1).IsNotNull();
        await Assert.That(item1).IsSameReferenceAs(item2);
        await Assert.That(playerService.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task GetOrCreatePlaybackItem_DuplicateMediaInDifferentQueueSlots_BuffersIndependently()
    {
        var playerService = new MockPlayerService();
        var queueService = new QueueService(null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, queueService);
        var player = new TestMediaPlayer();
        var sharedMedia = CreateMedia("shared");
        var slot1 = new QueueItemViewModel(sharedMedia);
        var slot2 = new QueueItemViewModel(sharedMedia);

        var playback1 = bufferManager.GetOrCreatePlaybackItem(player, slot1);
        var playback2 = bufferManager.GetOrCreatePlaybackItem(player, slot2);

        await Assert.That(playback1).IsNotNull();
        await Assert.That(playback2).IsNotNull();
        await Assert.That(playback1).IsNotSameReferenceAs(playback2);
        await Assert.That(playerService.CreateCount).IsEqualTo(2);
    }

    [Test]
    public async Task CleanPlaybackItem_DisposesAndRemovesItem()
    {
        var playerService = new MockPlayerService();
        var queueService = new QueueService(null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, queueService);
        var player = new TestMediaPlayer();
        var queueItem = CreateQueueItem("test1");

        var item1 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, queueItem)!;
        await Assert.That(item1.IsDisposed).IsFalse();

        bufferManager.CleanPlaybackItem(queueItem);
        await Assert.That(item1.IsDisposed).IsTrue();

        var item2 = bufferManager.GetOrCreatePlaybackItem(player, queueItem);
        await Assert.That(item2).IsNotSameReferenceAs(item1);
        await Assert.That(playerService.CreateCount).IsEqualTo(2);
    }

    [Test]
    public async Task CleanPlaybackItems_EvictsMatchingMediaSlotsExceptExcluded()
    {
        var playerService = new MockPlayerService();
        var queueService = new QueueService(null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, queueService);
        var player = new TestMediaPlayer();
        var sharedMedia = CreateMedia("shared");
        var otherMedia = CreateMedia("other");
        var activeSlot = new QueueItemViewModel(sharedMedia);
        var queuedDuplicateSlot = new QueueItemViewModel(sharedMedia);
        var otherSlot = new QueueItemViewModel(otherMedia);

        var activePlayback = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, activeSlot)!;
        var duplicatePlayback = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, queuedDuplicateSlot)!;
        var otherPlayback = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, otherSlot)!;

        bufferManager.CleanPlaybackItems(sharedMedia, exclude: activeSlot);

        await Assert.That(activePlayback.IsDisposed).IsFalse();
        await Assert.That(duplicatePlayback.IsDisposed).IsTrue();
        await Assert.That(otherPlayback.IsDisposed).IsFalse();
        await Assert.That(bufferManager.TryGetPlaybackItem(activeSlot, out _)).IsTrue();
        await Assert.That(bufferManager.TryGetPlaybackItem(queuedDuplicateSlot, out _)).IsFalse();
        await Assert.That(bufferManager.TryGetPlaybackItem(otherSlot, out _)).IsTrue();
    }

    [Test]
    public async Task Clear_DisposesAllCachedItems()
    {
        var playerService = new MockPlayerService();
        var queueService = new QueueService(null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, queueService);
        var player = new TestMediaPlayer();
        var slot1 = CreateQueueItem("test1");
        var slot2 = CreateQueueItem("test2");

        var item1 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, slot1)!;
        var item2 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, slot2)!;

        bufferManager.Clear();

        await Assert.That(item1.IsDisposed).IsTrue();
        await Assert.That(item2.IsDisposed).IsTrue();
        await Assert.That(bufferManager.BufferedItems).IsEmpty();
    }

    [Test]
    public async Task UpdateBuffer_EvictsOutOfWindowItemsAndExposesBufferedItems()
    {
        var playerService = new MockPlayerService();
        var queueService = new QueueService(null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, queueService);
        var player = new TestMediaPlayer();

        var items = new List<QueueItemViewModel>
        {
            CreateQueueItem("song0"),
            CreateQueueItem("song1"),
            CreateQueueItem("song2"),
            CreateQueueItem("song3"),
            CreateQueueItem("song4")
        };

        var queue = new QueueSession(items[0], items);

        // At index 0, buffer window is [0, 1, 2]
        bufferManager.UpdateBuffer(player, queue, MediaPlaybackAutoRepeatMode.None);
        var item0 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, items[0])!;
        var item1 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, items[1])!;
        await Assert.That(item0.IsDisposed).IsFalse();
        await Assert.That(item1.IsDisposed).IsFalse();
        await Assert.That(bufferManager.BufferedItems).Count().IsEqualTo(3);
        await Assert.That(bufferManager.BufferedItems[0]).IsSameReferenceAs(items[0]);
        await Assert.That(bufferManager.BufferedItems[1]).IsSameReferenceAs(items[1]);
        await Assert.That(bufferManager.BufferedItems[2]).IsSameReferenceAs(items[2]);

        // Slide window to index 4: buffer window is [2, 3, 4]
        queue.CurrentIndex = 4;
        bufferManager.UpdateBuffer(player, queue, MediaPlaybackAutoRepeatMode.None);

        // Items 0 and 1 slid out of window -> must be disposed
        await Assert.That(item0.IsDisposed).IsTrue();
        await Assert.That(item1.IsDisposed).IsTrue();
        await Assert.That(bufferManager.BufferedItems).Count().IsEqualTo(3);
        await Assert.That(bufferManager.BufferedItems[0]).IsSameReferenceAs(items[2]);
        await Assert.That(bufferManager.BufferedItems[1]).IsSameReferenceAs(items[3]);
        await Assert.That(bufferManager.BufferedItems[2]).IsSameReferenceAs(items[4]);
    }

    [Test]
    public async Task TryGetPlaybackItem_WhenBuffered_ReturnsTrueAndItemWithoutCreatingNew()
    {
        var playerService = new MockPlayerService();
        var queueService = new QueueService(null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, queueService);
        var player = new TestMediaPlayer();
        var media = CreateMedia("test1");
        var queueItem = new QueueItemViewModel(media);

        await Assert.That(bufferManager.TryGetPlaybackItem(queueItem, out var unbufferedSlot)).IsFalse();
        await Assert.That(unbufferedSlot).IsNull();
        await Assert.That(bufferManager.TryGetPlaybackItem(media, out var unbufferedMedia)).IsFalse();
        await Assert.That(unbufferedMedia).IsNull();
        await Assert.That(playerService.CreateCount).IsEqualTo(0);

        var created = bufferManager.GetOrCreatePlaybackItem(player, queueItem);
        await Assert.That(bufferManager.TryGetPlaybackItem(queueItem, out var bufferedSlot)).IsTrue();
        await Assert.That(bufferedSlot).IsSameReferenceAs(created);
        await Assert.That(bufferManager.TryGetPlaybackItem(media, out var bufferedMedia)).IsTrue();
        await Assert.That(bufferedMedia).IsSameReferenceAs(created);
        await Assert.That(playerService.CreateCount).IsEqualTo(1);
    }
}

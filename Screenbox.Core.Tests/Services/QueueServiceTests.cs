using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Factories;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media;
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;

namespace Screenbox.Core.Tests.Services;

public class QueueServiceTests
{
    private sealed class DummyExpandableMediaSource : IExpandableMediaSource
    {
        private readonly IReadOnlyList<ExpandedMediaItem> _items;

        public DummyExpandableMediaSource(IReadOnlyList<ExpandedMediaItem> items)
        {
            _items = items;
        }

        public string Location => "C:\\Playlists\\test.m3u";
        public string Name => "test.m3u";
        public MediaPlaybackType MediaType => MediaPlaybackType.Playlist;
        public DateTimeOffset DateAdded => DateTimeOffset.Now;

        public Task<IReadOnlyList<ExpandedMediaItem>> ExpandAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_items);
    }

    private readonly QueueService _service = new(new MediaViewModelFactory(new MediaSourceFactory(), new LibraryContext()), null!);

    private static MediaViewModel CreateMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task ParseQueueAsync_NonExpandableMedia_ReturnsSingleItemQueue()
    {
        var item = CreateMedia("song1");
        var queue = await _service.ParseQueueAsync(item);

        await Assert.That(queue.Items.Count).IsEqualTo(1);
        await Assert.That(queue.CurrentIndex).IsEqualTo(0);
        await Assert.That(queue.CurrentItem).IsSameReferenceAs(item);
    }

    [Test]
    public async Task ParseQueueAsync_ExpandableMediaWithItems_ReturnsExpandedQueue()
    {
        var expandedItems = new List<ExpandedMediaItem>
        {
            new(new UriMediaSource(new Uri("https://example.com/1.mp3")), "Track 1", TimeSpan.FromMinutes(3)),
            new(new UriMediaSource(new Uri("https://example.com/2.mp3")), "Track 2", TimeSpan.FromMinutes(4))
        };
        var expandableVm = new MediaViewModel(new DummyExpandableMediaSource(expandedItems));

        var queue = await _service.ParseQueueAsync(expandableVm);

        await Assert.That(queue.Items.Count).IsEqualTo(2);
        await Assert.That(queue.CurrentIndex).IsEqualTo(0);
        await Assert.That(queue.Items[0].Name).IsEqualTo("Track 1");
        await Assert.That(queue.Items[1].Name).IsEqualTo("Track 2");
    }

    [Test]
    public async Task ParseQueueAsync_ExpandableMediaEmpty_FallsBackToSingleItemQueue()
    {
        var expandableVm = new MediaViewModel(new DummyExpandableMediaSource(Array.Empty<ExpandedMediaItem>()));

        var queue = await _service.ParseQueueAsync(expandableVm);

        await Assert.That(queue.Items.Count).IsEqualTo(1);
        await Assert.That(queue.CurrentIndex).IsEqualTo(0);
        await Assert.That(queue.CurrentItem).IsSameReferenceAs(expandableVm);
    }

    [Test]
    public async Task ParseQueueAsync_Uri_CreatesQueueWithUriItem()
    {
        var uri = new Uri("https://example.com/stream.mp3");
        var queue = await _service.ParseQueueAsync(uri);

        await Assert.That(queue.Items.Count).IsEqualTo(1);
        await Assert.That(queue.CurrentIndex).IsEqualTo(0);
        await Assert.That(queue.CurrentItem!.Location).IsEqualTo("https://example.com/stream.mp3");
    }

    [Test]
    public async Task Shuffle_SetsShuffleModeAndPopulatesBackup()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var item3 = CreateMedia("song3");
        var queue = new QueueSession(item1, new List<MediaViewModel> { item1, item2, item3 });

        var shuffled = _service.Shuffle(queue, preserveIndex: 0);

        await Assert.That(shuffled.ShuffleMode).IsTrue();
        await Assert.That(shuffled.CanRestoreFromShuffle).IsTrue();
        await Assert.That(shuffled.Items[0]).IsEqualTo(item1);

        var restored = _service.RestoreFromShuffle(shuffled);
        await Assert.That(restored.Items.Count).IsEqualTo(3);
    }

    [Test]
    public async Task RestoreFromShuffle_RestoresOriginalOrder()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var item3 = CreateMedia("song3");
        var queue = new QueueSession(item1, new List<MediaViewModel> { item1, item2, item3 });

        var shuffled = _service.Shuffle(queue);
        var restored = _service.RestoreFromShuffle(shuffled);

        await Assert.That(restored.Items[0]).IsEqualTo(item1);
        await Assert.That(restored.Items[1]).IsEqualTo(item2);
        await Assert.That(restored.Items[2]).IsEqualTo(item3);
    }

    [Test]
    public async Task UpdateShuffleBackup_RemoveAction_RecordsRemoval()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var item3 = CreateMedia("song3");
        var queue = new QueueSession(item1, new List<MediaViewModel> { item1, item2, item3 });
        var shuffled = _service.Shuffle(queue);

        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item2, 1);
        _service.UpdateShuffleBackup(shuffled, args);

        var restored = _service.RestoreFromShuffle(shuffled);
        await Assert.That(restored.Items.Contains(item2)).IsFalse();
        await Assert.That(restored.Items.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UpdateShuffleBackup_ReplaceAction_UpdatesOriginalItem()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var queue = new QueueSession(item1, new List<MediaViewModel> { item1, item2 });
        var shuffled = _service.Shuffle(queue);

        var newItem = CreateMedia("song1_replaced");
        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, item1, 0);
        _service.UpdateShuffleBackup(shuffled, args);

        var restored = _service.RestoreFromShuffle(shuffled);
        await Assert.That(restored.Items.Contains(newItem)).IsTrue();
        await Assert.That(restored.Items.Contains(item1)).IsFalse();
    }

    [Test]
    public async Task UpdateShuffleBackup_ResetAction_ClearsBackup()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var queue = new QueueSession(item1, new List<MediaViewModel> { item1, item2 });
        var shuffled = _service.Shuffle(queue);

        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
        _service.UpdateShuffleBackup(shuffled, args);

        await Assert.That(shuffled.CanRestoreFromShuffle).IsFalse();
    }

    [Test]
    public async Task GetMediaBufferIndices_ReturnsExpectedRangeAroundCurrent()
    {
        var indices = _service.GetMediaBufferIndices(currentIndex: 3, queueCount: 10, MediaPlaybackAutoRepeatMode.None);
        await Assert.That(indices.Count).IsEqualTo(5);
        await Assert.That(indices[0]).IsEqualTo(1);
        await Assert.That(indices[1]).IsEqualTo(2);
        await Assert.That(indices[2]).IsEqualTo(3);
        await Assert.That(indices[3]).IsEqualTo(4);
        await Assert.That(indices[4]).IsEqualTo(5);
    }
}

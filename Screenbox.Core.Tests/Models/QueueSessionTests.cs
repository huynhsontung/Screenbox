using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.Models;

public class QueueSessionTests
{
    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task QueueSession_SingleItemConstructor_SetsCurrentIndexToZeroAndCurrentItem()
    {
        var item = CreateDummyMedia("song1");
        var queue = new QueueSession(item);

        await Assert.That(queue.Items.Count).IsEqualTo(1);
        await Assert.That(queue.CurrentIndex).IsEqualTo(0);
        await Assert.That(queue.CurrentItem?.Media).IsEqualTo(item);
        await Assert.That(queue.IsEmpty).IsFalse();
    }

    [Test]
    public async Task QueueSession_SingleItemConstructor_CopiesPropertiesFromReference()
    {
        var reference = new QueueSession
        {
            ShuffleMode = true
        };

        var item = CreateDummyMedia("song1");
        var queue = new QueueSession(item, reference);

        await Assert.That(queue.Items.Count).IsEqualTo(1);
        await Assert.That(queue.CurrentIndex).IsEqualTo(0);
        await Assert.That(queue.CurrentItem?.Media).IsEqualTo(item);
        await Assert.That(queue.ShuffleMode).IsTrue();
    }

    [Test]
    public async Task QueueSession_DefaultConstructor_IsEmpty()
    {
        var queue = new QueueSession();

        await Assert.That(queue.IsEmpty).IsTrue();
        await Assert.That(queue.Items.Count).IsEqualTo(0);
        await Assert.That(queue.CurrentIndex).IsEqualTo(-1);
        await Assert.That(queue.CurrentItem).IsNull();
    }

    [Test]
    public async Task QueueSession_ItemsConstructor_SetsCurrentIndexToNegativeOne()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var queue = new QueueSession(new[] { item1, item2 });

        await Assert.That(queue.Items.Count).IsEqualTo(2);
        await Assert.That(queue.CurrentIndex).IsEqualTo(-1);
        await Assert.That(queue.CurrentItem).IsNull();
    }

    [Test]
    public async Task QueueSession_CurrentItemConstructor_SetsCurrentIndexToMatch()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var queue = new QueueSession(item2, new[] { item1, item2 });

        await Assert.That(queue.CurrentIndex).IsEqualTo(1);
        await Assert.That(queue.CurrentItem?.Media).IsEqualTo(item2);
    }

    [Test]
    public async Task QueueSession_CopyConstructor_PreservesSessionIdAndOrigin()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA");
        var original = new QueueSession(0, new[] { item1, item2 })
        {
            Origin = origin
        };

        var copy = new QueueSession(1, new[] { item1, item2 }, original);

        await Assert.That(copy.SessionId).IsEqualTo(original.SessionId);
        await Assert.That(copy.Origin).IsSameReferenceAs(origin);
    }

    [Test]
    public async Task QueueSession_GetCanonicalItems_ReturnsOriginalOrderMinusRemovalsWhenShuffled()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var item3 = CreateDummyMedia("song3");
        var original = new QueueSession(0, new[] { item1, item2, item3 });
        var removedSlot = original.Items[1];

        var queueService = new QueueService(null!, null!);
        var shuffled = queueService.Shuffle(original);
        shuffled.Items.Remove(removedSlot);
        queueService.UpdateShuffleBackup(
            shuffled,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedSlot));

        var canonical = shuffled.GetCanonicalItems();

        await Assert.That(canonical.Count).IsEqualTo(2);
        await Assert.That(canonical[0].Media).IsEqualTo(item1);
        await Assert.That(canonical[1].Media).IsEqualTo(item3);
    }

    [Test]
    public async Task QueueSession_DuplicateMediaSlots_TrackDistinctIndicesAndRemovals()
    {
        var media = CreateDummyMedia("song1");
        var slot1 = new QueueItemViewModel(media);
        var slot2 = new QueueItemViewModel(media);
        var session = new QueueSession(slot2, new[] { slot1, slot2 });

        await Assert.That(session.CurrentIndex).IsEqualTo(1);
        await Assert.That(session.CurrentItem).IsSameReferenceAs(slot2);

        var queueService = new QueueService(null!, null!);
        var shuffled = queueService.Shuffle(session);
        shuffled.Items.Remove(slot1);
        queueService.UpdateShuffleBackup(
            shuffled,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, slot1));

        var canonical = shuffled.GetCanonicalItems();

        await Assert.That(canonical.Count).IsEqualTo(1);
        await Assert.That(canonical[0]).IsSameReferenceAs(slot2);
        await Assert.That(canonical[0].Media).IsSameReferenceAs(media);
    }

    [Test]
    public async Task QueueOrigin_ComputeContentHash_MatchesIdenticalMediaCollectionAndDiffersWhenOrderOrItemsChange()
    {
        string hash1 = QueueOrigin.ComputeContentHash(
            RecentItemKind.MediaCollection,
            @"C:\Folder",
            new[] { @"C:\Folder\a.mp3", @"C:\Folder\b.mp3" });

        string hashSameCaseInsensitive = QueueOrigin.ComputeContentHash(
            RecentItemKind.MediaCollection,
            @"c:\folder",
            new[] { @"c:\folder\A.MP3", @"c:\folder\B.MP3" });

        string hashDifferentOrder = QueueOrigin.ComputeContentHash(
            RecentItemKind.MediaCollection,
            @"C:\Folder",
            new[] { @"C:\Folder\b.mp3", @"C:\Folder\a.mp3" });

        string hashExtraFile = QueueOrigin.ComputeContentHash(
            RecentItemKind.MediaCollection,
            @"C:\Folder",
            new[] { @"C:\Folder\a.mp3", @"C:\Folder\b.mp3", @"C:\Folder\c.mp3" });

        await Assert.That(hash1).IsEqualTo(hashSameCaseInsensitive);
        await Assert.That(hash1).IsNotEqualTo(hashDifferentOrder);
        await Assert.That(hash1).IsNotEqualTo(hashExtraFile);
    }
}

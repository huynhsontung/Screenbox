using System;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
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
    public async Task QueueSession_CopyConstructor_PreservesSessionIdAndItemOrigins()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA");
        var original = new QueueSession(0, new[] { item1, item2 }, origin: origin);

        var copy = new QueueSession(1, original.Items, original);

        await Assert.That(copy.SessionId).IsEqualTo(original.SessionId);
        await Assert.That(copy.Items[0].Origin).IsSameReferenceAs(origin);
        await Assert.That(copy.Items[1].Origin).IsSameReferenceAs(origin);
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
        await Assert.That(session.CurrentItem?.Media).IsSameReferenceAs(media);
    }

    [Test]
    public async Task QueueOrigin_GetEntryId_MatchesCaseInsensitivelyByKindAndSource()
    {
        string id1 = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"C:\Folder");
        string idSameCaseInsensitive = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"c:\folder");
        string idDifferentFolder = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"C:\OtherFolder");
        string idDifferentKind = QueueOrigin.GetEntryId(RecentItemKind.Playlist, @"C:\Folder");

        await Assert.That(id1).IsEqualTo(idSameCaseInsensitive);
        await Assert.That(id1).IsNotEqualTo(idDifferentFolder);
        await Assert.That(id1).IsNotEqualTo(idDifferentKind);
    }

    [Test]
    public async Task QueueOrigin_TrackItemsIndividually_IsTrueForEmptyMediaCollectionAndSingleMedia()
    {
        var item = CreateDummyMedia("song1");
        var looseCollection = QueueOrigin.ForMediaCollection("Loose", null);
        var folderCollection = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA");
        var single = QueueOrigin.ForSingleMedia(item);

        await Assert.That(looseCollection.TrackItemsIndividually).IsTrue();
        await Assert.That(folderCollection.TrackItemsIndividually).IsFalse();
        await Assert.That(single.TrackItemsIndividually).IsTrue();
    }
}

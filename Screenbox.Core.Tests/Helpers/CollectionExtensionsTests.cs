using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Collections;
using Screenbox.Core.Helpers;

namespace Screenbox.Core.Tests.Helpers;

public class CollectionExtensionsTests
{
    private static List<NotifyCollectionChangedEventArgs> TrackChanges<T>(ObservableCollection<T> collection)
    {
        var events = new List<NotifyCollectionChangedEventArgs>();
        collection.CollectionChanged += (_, e) => events.Add(e);
        return events;
    }

    [Test]
    public async Task SyncItems_WhenItemRemovedAtMiddleIndex_ShouldEmitSingleRemoveEvent()
    {
        var target = new ObservableCollection<string> { "A", "B", "C", "D", "E" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "A", "B", "D", "E" });

        await Assert.That(target.SequenceEqual(new[] { "A", "B", "D", "E" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(events[0].OldStartingIndex).IsEqualTo(2);
        await Assert.That(events[0].OldItems![0]).IsEqualTo("C");
    }

    [Test]
    public async Task SyncItems_WhenItemRemovedAtFirstIndex_ShouldEmitSingleRemoveEvent()
    {
        var target = new ObservableCollection<string> { "A", "B", "C" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "B", "C" });

        await Assert.That(target.SequenceEqual(new[] { "B", "C" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(events[0].OldStartingIndex).IsEqualTo(0);
        await Assert.That(events[0].OldItems![0]).IsEqualTo("A");
    }

    [Test]
    public async Task SyncItems_WhenItemRemovedAtLastIndex_ShouldEmitSingleRemoveEvent()
    {
        var target = new ObservableCollection<string> { "A", "B", "C" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "A", "B" });

        await Assert.That(target.SequenceEqual(new[] { "A", "B" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(events[0].OldStartingIndex).IsEqualTo(2);
        await Assert.That(events[0].OldItems![0]).IsEqualTo("C");
    }

    [Test]
    public async Task SyncItems_WhenMultipleNonContiguousItemsRemoved_ShouldEmitOnlyRemoveEvents()
    {
        var target = new ObservableCollection<string> { "A", "B", "C", "D", "E", "F" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "A", "D", "F" });

        await Assert.That(target.SequenceEqual(new[] { "A", "D", "F" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(3);
        await Assert.That(events.All(e => e.Action == NotifyCollectionChangedAction.Remove)).IsTrue();
        await Assert.That(events[0].OldStartingIndex).IsEqualTo(4);
        await Assert.That(events[1].OldStartingIndex).IsEqualTo(2);
        await Assert.That(events[2].OldStartingIndex).IsEqualTo(1);
    }

    [Test]
    public async Task SyncItems_WhenReferenceIsEmpty_ShouldRemoveAllItems()
    {
        var target = new ObservableCollection<string> { "A", "B", "C" };
        var events = TrackChanges(target);

        target.SyncItems(Array.Empty<string>());

        await Assert.That(target.Count).IsEqualTo(0);
        await Assert.That(events.Count).IsEqualTo(3);
        await Assert.That(events.All(e => e.Action == NotifyCollectionChangedAction.Remove)).IsTrue();
    }

    [Test]
    public async Task SyncItems_WhenTargetIsEmpty_ShouldAddAllItems()
    {
        var target = new ObservableCollection<string>();
        var events = TrackChanges(target);

        target.SyncItems(new[] { "A", "B", "C" });

        await Assert.That(target.SequenceEqual(new[] { "A", "B", "C" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(3);
        await Assert.That(events.All(e => e.Action == NotifyCollectionChangedAction.Add)).IsTrue();
    }

    [Test]
    public async Task SyncItems_WhenCollectionsAreIdentical_ShouldEmitNoEvents()
    {
        var target = new ObservableCollection<string> { "A", "B", "C" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "A", "B", "C" });

        await Assert.That(target.SequenceEqual(new[] { "A", "B", "C" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SyncItems_WhenTargetAndReferenceAreSameInstance_ShouldBeNoOp()
    {
        var target = new ObservableCollection<string> { "A", "B", "C" };
        var events = TrackChanges(target);

        target.SyncItems(target);

        await Assert.That(target.SequenceEqual(new[] { "A", "B", "C" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SyncItems_WhenItemInsertedInMiddle_ShouldEmitSingleAddEvent()
    {
        var target = new ObservableCollection<string> { "A", "B", "D" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "A", "B", "C", "D" });

        await Assert.That(target.SequenceEqual(new[] { "A", "B", "C", "D" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Action).IsEqualTo(NotifyCollectionChangedAction.Add);
        await Assert.That(events[0].NewStartingIndex).IsEqualTo(2);
        await Assert.That(events[0].NewItems![0]).IsEqualTo("C");
    }

    [Test]
    public async Task SyncItems_WhenItemReplacedInMiddle_ShouldNotTouchSubsequentItems()
    {
        var target = new ObservableCollection<string> { "A", "B", "C", "D" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "A", "X", "C", "D" });

        await Assert.That(target.SequenceEqual(new[] { "A", "X", "C", "D" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(2);
        await Assert.That(events[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(events[0].OldStartingIndex).IsEqualTo(1);
        await Assert.That(events[0].OldItems![0]).IsEqualTo("B");
        await Assert.That(events[1].Action).IsEqualTo(NotifyCollectionChangedAction.Add);
        await Assert.That(events[1].NewStartingIndex).IsEqualTo(1);
        await Assert.That(events[1].NewItems![0]).IsEqualTo("X");
    }

    [Test]
    public async Task SyncItems_WhenItemMovedToFront_ShouldEmitSingleRemoveAndSingleAdd()
    {
        var target = new ObservableCollection<string> { "A", "B", "C", "D" };
        var events = TrackChanges(target);

        target.SyncItems(new[] { "D", "A", "B", "C" });

        await Assert.That(target.SequenceEqual(new[] { "D", "A", "B", "C" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(2);
        await Assert.That(events[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(events[0].OldStartingIndex).IsEqualTo(3);
        await Assert.That(events[0].OldItems![0]).IsEqualTo("D");
        await Assert.That(events[1].Action).IsEqualTo(NotifyCollectionChangedAction.Add);
        await Assert.That(events[1].NewStartingIndex).IsEqualTo(0);
        await Assert.That(events[1].NewItems![0]).IsEqualTo("D");
    }

    [Test]
    public async Task SyncItems_WhenReversed_ShouldMatchReferenceOrder()
    {
        var target = new ObservableCollection<string> { "A", "B", "C", "D" };

        target.SyncItems(new[] { "D", "C", "B", "A" });

        await Assert.That(target.SequenceEqual(new[] { "D", "C", "B", "A" })).IsTrue();
    }

    [Test]
    public async Task SyncItems_WhenDuplicateAdded_ShouldNotCorruptEarlierIndices()
    {
        var target = new ObservableCollection<string> { "English", "Spanish" };

        target.SyncItems(new[] { "English", "English", "Spanish" });

        await Assert.That(target.SequenceEqual(new[] { "English", "English", "Spanish" })).IsTrue();
    }

    [Test]
    public async Task SyncItems_WhenExcessDuplicateRemoved_ShouldMatchReference()
    {
        var target = new ObservableCollection<string> { "English", "English", "Spanish" };

        target.SyncItems(new[] { "English", "Spanish" });

        await Assert.That(target.SequenceEqual(new[] { "English", "Spanish" })).IsTrue();
    }

    [Test]
    public async Task SyncItems_WhenItemsContainNull_ShouldSyncWithoutThrowing()
    {
        var target = new ObservableCollection<string?> { "A", null, "B", "C" };
        var events = TrackChanges(target);

        target.SyncItems(new string?[] { "A", "B", "C" });

        await Assert.That(target.SequenceEqual(new string?[] { "A", "B", "C" })).IsTrue();
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(events[0].OldStartingIndex).IsEqualTo(1);
    }

    [Test]
    public async Task SyncObservableGroups_WhenGroupAndItemRemoved_ShouldEmitMinimalEvents()
    {
        var groupA = new ObservableGroup<string, string>("A", new[] { "A1", "A2", "A3" });
        var groupB = new ObservableGroup<string, string>("B", new[] { "B1" });
        var groupC = new ObservableGroup<string, string>("C", new[] { "C1", "C2" });
        var target = new ObservableCollection<ObservableGroup<string, string>> { groupA, groupB, groupC };

        var groupEvents = TrackChanges(target);
        var groupAEvents = TrackChanges(groupA);

        var reference = new List<IGrouping<string, string>>
        {
            new ObservableGroup<string, string>("A", new[] { "A1", "A3" }),
            new ObservableGroup<string, string>("C", new[] { "C1", "C2" })
        };

        target.SyncObservableGroups(reference);

        await Assert.That(target.Select(g => g.Key).SequenceEqual(new[] { "A", "C" })).IsTrue();
        await Assert.That(groupEvents.Count).IsEqualTo(1);
        await Assert.That(groupEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(groupEvents[0].OldStartingIndex).IsEqualTo(1);

        await Assert.That(groupA.SequenceEqual(new[] { "A1", "A3" })).IsTrue();
        await Assert.That(groupAEvents.Count).IsEqualTo(1);
        await Assert.That(groupAEvents[0].Action).IsEqualTo(NotifyCollectionChangedAction.Remove);
        await Assert.That(groupAEvents[0].OldStartingIndex).IsEqualTo(1);
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media;

namespace Screenbox.Core.Tests.Services;

public class PlaylistServiceTests
{
    private readonly PlaylistService _service = new(null!, null!);

    private static MediaViewModel CreateMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task ShufflePlaylist_SetsShuffleModeAndPopulatesBackup()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var item3 = CreateMedia("song3");
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2, item3 });

        var shuffled = _service.ShufflePlaylist(playlist, preserveIndex: 0);

        await Assert.That(shuffled.ShuffleMode).IsTrue();
        await Assert.That(shuffled.ShuffleBackup).IsNotNull();
        await Assert.That(shuffled.ShuffleBackup!.OriginalPlaylist.Count).IsEqualTo(3);
        await Assert.That(shuffled.Items[0]).IsEqualTo(item1);
    }

    [Test]
    public async Task RestoreFromShuffle_RestoresOriginalOrder()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var item3 = CreateMedia("song3");
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2, item3 });

        var shuffled = _service.ShufflePlaylist(playlist);
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
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2, item3 });
        var shuffled = _service.ShufflePlaylist(playlist);

        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item2, 1);
        _service.UpdateShuffleBackup(shuffled, args);

        await Assert.That(shuffled.ShuffleBackup!.Removals.Contains(item2)).IsTrue();

        var restored = _service.RestoreFromShuffle(shuffled);
        await Assert.That(restored.Items.Contains(item2)).IsFalse();
        await Assert.That(restored.Items.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UpdateShuffleBackup_ReplaceAction_UpdatesOriginalItem()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2 });
        var shuffled = _service.ShufflePlaylist(playlist);

        var newItem = CreateMedia("song1_replaced");
        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, item1, 0);
        _service.UpdateShuffleBackup(shuffled, args);

        await Assert.That(shuffled.ShuffleBackup!.OriginalPlaylist.Contains(newItem)).IsTrue();
        await Assert.That(shuffled.ShuffleBackup.OriginalPlaylist.Contains(item1)).IsFalse();
    }

    [Test]
    public async Task UpdateShuffleBackup_ResetAction_ClearsBackup()
    {
        var item1 = CreateMedia("song1");
        var item2 = CreateMedia("song2");
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2 });
        var shuffled = _service.ShufflePlaylist(playlist);

        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
        _service.UpdateShuffleBackup(shuffled, args);

        await Assert.That(shuffled.ShuffleBackup).IsNull();
    }

    [Test]
    public async Task GetMediaBufferIndices_ReturnsExpectedRangeAroundCurrent()
    {
        var indices = _service.GetMediaBufferIndices(currentIndex: 3, playlistCount: 10, MediaPlaybackAutoRepeatMode.None);
        await Assert.That(indices.Count).IsEqualTo(5);
        await Assert.That(indices[0]).IsEqualTo(1);
        await Assert.That(indices[1]).IsEqualTo(2);
        await Assert.That(indices[2]).IsEqualTo(3);
        await Assert.That(indices[3]).IsEqualTo(4);
        await Assert.That(indices[4]).IsEqualTo(5);
    }
}

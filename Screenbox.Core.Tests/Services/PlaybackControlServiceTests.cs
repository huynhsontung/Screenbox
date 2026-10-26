using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage.Search;

namespace Screenbox.Core.Tests.Services;

public class PlaybackControlServiceTests
{
    private readonly PlaybackControlService _service = new(null!, null!);

    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private static StorageFileQueryResult CreateDummyQuery() =>
        Unsafe.As<StorageFileQueryResult>(RuntimeHelpers.GetUninitializedObject(typeof(StorageFileQueryResult)));

    [Test]
    public async Task CanNext_EmptyPlaylist_ReturnsFalse()
    {
        var playlist = new Playlist();
        await Assert.That(_service.CanNext(playlist)).IsFalse();
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.List)).IsFalse();

        playlist.NeighboringFilesQuery = CreateDummyQuery();
        await Assert.That(_service.CanNext(playlist)).IsFalse();
    }

    [Test]
    public async Task CanNext_SingleItem_RequiresNeighborOrListRepeat()
    {
        var item = CreateDummyMedia("song1");
        var playlist = new Playlist(item, new List<MediaViewModel> { item });

        // Single item without neighbor or list repeat -> cannot go next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None)).IsFalse();

        // Single item with list repeat -> can go next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.List)).IsTrue();

        // Single item with neighbor in folder -> can go next
        playlist.NeighboringFilesQuery = CreateDummyQuery();
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None)).IsTrue();
    }

    [Test]
    public async Task CanNext_MultiItem_DisablesAtEndRegardlessOfNeighborQuery()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2 });

        // At index 0 of 2 -> can go next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None)).IsTrue();

        // Advance to index 1 of 2 (last item)
        playlist.CurrentIndex = 1;
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None)).IsFalse();

        // At last item, NeighboringFilesQuery should NOT allow going next (multi-item playlist ignores neighboring folder query)
        playlist.NeighboringFilesQuery = CreateDummyQuery();
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None)).IsFalse();

        // With list repeat, can always cycle next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.List)).IsTrue();
    }

    [Test]
    public async Task CanPrevious_ChecksPlaylistNonEmpty()
    {
        var emptyPlaylist = new Playlist();
        await Assert.That(_service.CanPrevious(emptyPlaylist)).IsFalse();

        var item = CreateDummyMedia("song1");
        var playlist = new Playlist(item, new List<MediaViewModel> { item });
        await Assert.That(_service.CanPrevious(playlist)).IsTrue();
    }

    [Test]
    public async Task GetNextAsync_MultiItemPlaylist_NavigatesToNextItem()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2 });

        var result = await _service.GetNextAsync(playlist);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.NextItem).IsEqualTo(item2);
    }

    [Test]
    public async Task GetPreviousAsync_MultiItemPlaylist_NavigatesToPreviousItem()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var playlist = new Playlist(item2, new List<MediaViewModel> { item1, item2 });

        var result = await _service.GetPreviousAsync(playlist);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.NextItem).IsEqualTo(item1);
    }
}

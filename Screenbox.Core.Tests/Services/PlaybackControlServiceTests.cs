using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media;

namespace Screenbox.Core.Tests.Services;

public class PlaybackControlServiceTests
{
    private readonly PlaybackControlService _service = new(null!, null!);

    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task CanNext_EmptyPlaylist_ReturnsFalse()
    {
        var playlist = new Playlist();
        await Assert.That(_service.CanNext(playlist)).IsFalse();
        await Assert.That(_service.CanNext(playlist, hasNeighbor: true)).IsFalse();
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.List)).IsFalse();
    }

    [Test]
    public async Task CanNext_SingleItem_RequiresNeighborOrListRepeat()
    {
        var item = CreateDummyMedia("song1");
        var playlist = new Playlist(item, new List<MediaViewModel> { item });

        // Single item without neighbor or list repeat -> cannot go next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None, hasNeighbor: false)).IsFalse();

        // Single item with neighbor in folder -> can go next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None, hasNeighbor: true)).IsTrue();

        // Single item with list repeat -> can go next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.List, hasNeighbor: false)).IsTrue();
    }

    [Test]
    public async Task CanNext_MultiItem_DisablesAtEndRegardlessOfNeighborFlag()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var playlist = new Playlist(item1, new List<MediaViewModel> { item1, item2 });

        // At index 0 of 2 -> can go next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None, hasNeighbor: false)).IsTrue();

        // Advance to index 1 of 2 (last item)
        playlist.CurrentIndex = 1;

        // At last item, hasNeighbor: true should NOT allow going next (multi-item playlist ignores neighboring folder query)
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None, hasNeighbor: true)).IsFalse();
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.None, hasNeighbor: false)).IsFalse();

        // With list repeat, can always cycle next
        await Assert.That(_service.CanNext(playlist, MediaPlaybackAutoRepeatMode.List, hasNeighbor: false)).IsTrue();
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
}

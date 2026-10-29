using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.Models;

public class PlaylistTests
{
    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task Playlist_SingleItemConstructor_SetsCurrentIndexToZeroAndCurrentItem()
    {
        var item = CreateDummyMedia("song1");
        var playlist = new Playlist(item);

        await Assert.That(playlist.Items.Count).IsEqualTo(1);
        await Assert.That(playlist.CurrentIndex).IsEqualTo(0);
        await Assert.That(playlist.CurrentItem).IsEqualTo(item);
        await Assert.That(playlist.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Playlist_SingleItemConstructor_CopiesPropertiesFromReference()
    {
        var reference = new Playlist
        {
            ShuffleMode = true
        };

        var item = CreateDummyMedia("song1");
        var playlist = new Playlist(item, reference);

        await Assert.That(playlist.Items.Count).IsEqualTo(1);
        await Assert.That(playlist.CurrentIndex).IsEqualTo(0);
        await Assert.That(playlist.CurrentItem).IsEqualTo(item);
        await Assert.That(playlist.ShuffleMode).IsTrue();
    }

    [Test]
    public async Task Playlist_DefaultConstructor_IsEmpty()
    {
        var playlist = new Playlist();

        await Assert.That(playlist.IsEmpty).IsTrue();
        await Assert.That(playlist.Items.Count).IsEqualTo(0);
        await Assert.That(playlist.CurrentIndex).IsEqualTo(-1);
        await Assert.That(playlist.CurrentItem).IsNull();
    }

    [Test]
    public async Task Playlist_ItemsConstructor_SetsCurrentIndexToNegativeOne()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var playlist = new Playlist(new[] { item1, item2 });

        await Assert.That(playlist.Items.Count).IsEqualTo(2);
        await Assert.That(playlist.CurrentIndex).IsEqualTo(-1);
        await Assert.That(playlist.CurrentItem).IsNull();
    }

    [Test]
    public async Task Playlist_CurrentItemConstructor_SetsCurrentIndexToMatch()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var playlist = new Playlist(item2, new[] { item1, item2 });

        await Assert.That(playlist.CurrentIndex).IsEqualTo(1);
        await Assert.That(playlist.CurrentItem).IsEqualTo(item2);
    }
}

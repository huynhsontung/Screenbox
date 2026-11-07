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

    [Test]
    public async Task Playlist_CopyConstructor_PreservesSessionIdAndOrigin()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var origin = PlaylistOrigin.ForAdHocPlaylist("FolderA", @"C:\FolderA");
        var original = new Playlist(0, new[] { item1, item2 })
        {
            Origin = origin
        };

        var copy = new Playlist(1, new[] { item1, item2 }, original);

        await Assert.That(copy.SessionId).IsEqualTo(original.SessionId);
        await Assert.That(copy.Origin).IsSameReferenceAs(origin);
    }

    [Test]
    public async Task Playlist_GetCanonicalItems_ReturnsOriginalOrderMinusRemovalsWhenShuffled()
    {
        var item1 = CreateDummyMedia("song1");
        var item2 = CreateDummyMedia("song2");
        var item3 = CreateDummyMedia("song3");
        var original = new Playlist(0, new[] { item1, item2, item3 });

        var playlistService = new PlaylistService(null!, null!, null!);
        var shuffled = playlistService.ShufflePlaylist(original);
        shuffled.Items.Remove(item2);
        playlistService.UpdateShuffleBackup(
            shuffled,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item2));

        var canonical = shuffled.GetCanonicalItems();

        await Assert.That(canonical.Count).IsEqualTo(2);
        await Assert.That(canonical[0]).IsEqualTo(item1);
        await Assert.That(canonical[1]).IsEqualTo(item3);
    }

    [Test]
    public async Task PlaylistOrigin_ComputeContentHash_MatchesIdenticalAdHocAndDiffersWhenOrderOrItemsChange()
    {
        string hash1 = PlaylistOrigin.ComputeContentHash(
            RecentItemKind.AdHocPlaylist,
            @"C:\Folder",
            new[] { @"C:\Folder\a.mp3", @"C:\Folder\b.mp3" });

        string hashSameCaseInsensitive = PlaylistOrigin.ComputeContentHash(
            RecentItemKind.AdHocPlaylist,
            @"c:\folder",
            new[] { @"c:\folder\A.MP3", @"c:\folder\B.MP3" });

        string hashDifferentOrder = PlaylistOrigin.ComputeContentHash(
            RecentItemKind.AdHocPlaylist,
            @"C:\Folder",
            new[] { @"C:\Folder\b.mp3", @"C:\Folder\a.mp3" });

        string hashExtraFile = PlaylistOrigin.ComputeContentHash(
            RecentItemKind.AdHocPlaylist,
            @"C:\Folder",
            new[] { @"C:\Folder\a.mp3", @"C:\Folder\b.mp3", @"C:\Folder\c.mp3" });

        await Assert.That(hash1).IsEqualTo(hashSameCaseInsensitive);
        await Assert.That(hash1).IsNotEqualTo(hashDifferentOrder);
        await Assert.That(hash1).IsNotEqualTo(hashExtraFile);
    }
}

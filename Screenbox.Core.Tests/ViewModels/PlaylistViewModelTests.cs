using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class PlaylistViewModelTests
{
    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private static PlaylistViewModel CreatePlaylist() =>
        new(null!, null!, NullLogger<PlaylistViewModel>.Instance, new WeakReferenceMessenger())
        {
            Name = "Favorites"
        };

    [Test]
    public async Task AddItemsAsync_DuplicateMedia_WrapsCanonicalMediaInDistinctEntries()
    {
        var playlist = CreatePlaylist();
        var media = CreateDummyMedia("song1");

        await playlist.AddItemsAsync(new[] { media, media });

        await Assert.That(playlist.Items.Count).IsEqualTo(2);
        await Assert.That(playlist.Items[0]).IsNotSameReferenceAs(playlist.Items[1]);
        await Assert.That(playlist.Items[0].Media).IsSameReferenceAs(media);
        await Assert.That(playlist.Items[1].Media).IsSameReferenceAs(media);
    }

    [Test]
    public async Task AddItemsAtIndexAsync_InsertsDuplicateAtTargetIndexWithoutCloningMedia()
    {
        var playlist = CreatePlaylist();
        var media1 = CreateDummyMedia("song1");
        var media2 = CreateDummyMedia("song2");

        await playlist.AddItemsAsync(new[] { media1, media2 });
        await playlist.AddItemsAtIndexAsync(new[] { media1 }, insertIndex: 1);

        await Assert.That(playlist.Items.Count).IsEqualTo(3);
        await Assert.That(playlist.Items[0].Media).IsSameReferenceAs(media1);
        await Assert.That(playlist.Items[1].Media).IsSameReferenceAs(media1);
        await Assert.That(playlist.Items[2].Media).IsSameReferenceAs(media2);
        await Assert.That(playlist.Items[0]).IsNotSameReferenceAs(playlist.Items[1]);
    }

    [Test]
    public async Task Remove_DuplicateEntry_RemovesOnlyTargetedEntry()
    {
        var playlist = CreatePlaylist();
        var media = CreateDummyMedia("song1");

        await playlist.AddItemsAsync(new[] { media, media });
        var firstEntry = playlist.Items[0];
        var secondEntry = playlist.Items[1];

        playlist.Items.Remove(firstEntry);

        await Assert.That(playlist.Items.Count).IsEqualTo(1);
        await Assert.That(playlist.Items[0]).IsSameReferenceAs(secondEntry);
        await Assert.That(playlist.Items[0].Media).IsSameReferenceAs(media);
    }

    [Test]
    public async Task ToQueueSession_WrapsCanonicalMediaInQueueItemsWithPlaylistOrigin()
    {
        var playlist = CreatePlaylist();
        var media1 = CreateDummyMedia("song1");
        var media2 = CreateDummyMedia("song2");

        await playlist.AddItemsAsync(new[] { media1, media2, media1 });
        var session = playlist.ToQueueSession();

        await Assert.That(session.Items.Count).IsEqualTo(3);
        await Assert.That(session.CurrentIndex).IsEqualTo(0);
        await Assert.That(session.Origin?.Kind).IsEqualTo(RecentItemKind.Playlist);
        await Assert.That(session.Origin?.SourceId).IsEqualTo(playlist.Id);
        await Assert.That(session.Items[0].Media).IsSameReferenceAs(media1);
        await Assert.That(session.Items[1].Media).IsSameReferenceAs(media2);
        await Assert.That(session.Items[2].Media).IsSameReferenceAs(media1);
        await Assert.That(session.Items[0]).IsNotSameReferenceAs(session.Items[2]);
    }
}

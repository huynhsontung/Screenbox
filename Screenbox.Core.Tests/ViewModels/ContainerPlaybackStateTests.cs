using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class ContainerPlaybackStateTests
{
    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task AlbumViewModel_QueuePlaybackStateChanged_UpdatesActiveAndPlayingWhenOriginMatches()
    {
        var messenger = new StrongReferenceMessenger();
        var album = new AlbumViewModel("Abbey Road", "The Beatles", messenger);
        var song = CreateDummyMedia("Come Together");
        var sessionId = Guid.NewGuid();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForAlbum(album), song, IsPlaying: true)));

        await Assert.That(album.IsQueueActive).IsTrue();
        await Assert.That(album.IsPlaying).IsTrue();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForAlbum(album), song, IsPlaying: false)));

        await Assert.That(album.IsQueueActive).IsTrue();
        await Assert.That(album.IsPlaying).IsFalse();
    }

    [Test]
    public async Task AlbumViewModel_QueuePlaybackStateChanged_RemainsInactiveWhenOriginDoesNotMatch()
    {
        var messenger = new StrongReferenceMessenger();
        var album = new AlbumViewModel("Abbey Road", "The Beatles", messenger);
        var song = CreateDummyMedia("Come Together");
        var sessionId = Guid.NewGuid();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForSingleMedia(song), song, IsPlaying: true)));

        await Assert.That(album.IsQueueActive).IsFalse();
        await Assert.That(album.IsPlaying).IsFalse();
    }

    [Test]
    public async Task ArtistViewModel_QueuePlaybackStateChanged_UpdatesActiveAndPlayingWhenOriginMatches()
    {
        var messenger = new StrongReferenceMessenger();
        var artist = new ArtistViewModel("The Beatles", messenger);
        var song = CreateDummyMedia("Come Together");
        var sessionId = Guid.NewGuid();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForArtist(artist), song, IsPlaying: true)));

        await Assert.That(artist.IsQueueActive).IsTrue();
        await Assert.That(artist.IsPlaying).IsTrue();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForArtist(artist), song, IsPlaying: false)));

        await Assert.That(artist.IsQueueActive).IsTrue();
        await Assert.That(artist.IsPlaying).IsFalse();
    }

    [Test]
    public async Task ArtistViewModel_QueuePlaybackStateChanged_RemainsInactiveWhenOriginDoesNotMatch()
    {
        var messenger = new StrongReferenceMessenger();
        var artist = new ArtistViewModel("The Beatles", messenger);
        var otherArtist = new ArtistViewModel("Queen", messenger);
        var song = CreateDummyMedia("Bohemian Rhapsody");
        var sessionId = Guid.NewGuid();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForArtist(otherArtist), song, IsPlaying: true)));

        await Assert.That(artist.IsQueueActive).IsFalse();
        await Assert.That(artist.IsPlaying).IsFalse();
    }

    [Test]
    public async Task PlaylistViewModel_QueuePlaybackStateChanged_UpdatesActiveAndPlayingWhenOriginMatches()
    {
        var messenger = new StrongReferenceMessenger();
        var playlist = new PlaylistViewModel(null!, null!, NullLogger<PlaylistViewModel>.Instance, messenger);
        var song = CreateDummyMedia("Track 1");
        var sessionId = Guid.NewGuid();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForUserPlaylist(playlist), song, IsPlaying: true)));

        await Assert.That(playlist.IsQueueActive).IsTrue();
        await Assert.That(playlist.IsPlaying).IsTrue();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForUserPlaylist(playlist), song, IsPlaying: false)));

        await Assert.That(playlist.IsQueueActive).IsTrue();
        await Assert.That(playlist.IsPlaying).IsFalse();
    }

    [Test]
    public async Task PlaylistViewModel_QueuePlaybackStateChanged_RemainsInactiveWhenOriginDoesNotMatch()
    {
        var messenger = new StrongReferenceMessenger();
        var playlist = new PlaylistViewModel(null!, null!, NullLogger<PlaylistViewModel>.Instance, messenger);
        var song = CreateDummyMedia("Track 1");
        var sessionId = Guid.NewGuid();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, PlaylistOrigin.ForSingleMedia(song), song, IsPlaying: true)));

        await Assert.That(playlist.IsQueueActive).IsFalse();
        await Assert.That(playlist.IsPlaying).IsFalse();
    }

    [Test]
    public async Task AlbumViewModel_PlayAlbum_WhenActive_TogglesPlayPause()
    {
        var messenger = new StrongReferenceMessenger();
        var album = new AlbumViewModel("Abbey Road", "The Beatles", messenger);
        var song = CreateDummyMedia("Come Together");
        album.RelatedSongs.Add(song);

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(Guid.NewGuid(), PlaylistOrigin.ForAlbum(album), song, IsPlaying: true)));

        bool toggleSent = false;
        messenger.Register<ContainerPlaybackStateTests, TogglePlayPauseMessage>(
            this,
            (_, _) => toggleSent = true);

        try
        {
            album.PlayAlbumCommand.Execute(null);
            await Assert.That(toggleSent).IsTrue();
        }
        finally
        {
            messenger.Unregister<TogglePlayPauseMessage>(this);
        }
    }
}

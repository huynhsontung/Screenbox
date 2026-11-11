using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
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
            new QueuePlaybackState(sessionId, QueueOrigin.ForAlbum(album), song, IsPlaying: true)));

        await Assert.That(album.IsQueueActive).IsTrue();
        await Assert.That(album.IsPlaying).IsTrue();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, QueueOrigin.ForAlbum(album), song, IsPlaying: false)));

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
            new QueuePlaybackState(sessionId, QueueOrigin.ForSingleMedia(song), song, IsPlaying: true)));

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
            new QueuePlaybackState(sessionId, QueueOrigin.ForArtist(artist), song, IsPlaying: true)));

        await Assert.That(artist.IsQueueActive).IsTrue();
        await Assert.That(artist.IsPlaying).IsTrue();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, QueueOrigin.ForArtist(artist), song, IsPlaying: false)));

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
            new QueuePlaybackState(sessionId, QueueOrigin.ForArtist(otherArtist), song, IsPlaying: true)));

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
            new QueuePlaybackState(sessionId, QueueOrigin.ForPlaylist(playlist), song, IsPlaying: true)));

        await Assert.That(playlist.IsQueueActive).IsTrue();
        await Assert.That(playlist.IsPlaying).IsTrue();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, QueueOrigin.ForPlaylist(playlist), song, IsPlaying: false)));

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
            new QueuePlaybackState(sessionId, QueueOrigin.ForSingleMedia(song), song, IsPlaying: true)));

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
            new QueuePlaybackState(Guid.NewGuid(), QueueOrigin.ForAlbum(album), song, IsPlaying: true)));

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

    [Test]
    public async Task AlbumViewModel_EmptyAlbumArtist_AlbumKeyRemainsStableAfterUpdateProperties()
    {
        var messenger = new StrongReferenceMessenger();
        var album = new AlbumViewModel("Abbey Road", string.Empty, messenger);
        string initialKey = album.AlbumKey;

        var artist = new ArtistViewModel("The Beatles", messenger);
        var song = CreateDummyMedia("Come Together");
        song.Artists = new[] { artist };
        album.RelatedSongs.Add(song);

        album.UpdateProperties();

        await Assert.That(album.ArtistName).IsEqualTo("The Beatles");
        await Assert.That(album.AlbumKey).IsEqualTo(initialKey);
    }

    [Test]
    public async Task SendQueueAndPlay_ActiveMediaWithDifferentOrigin_QueuesNewSessionInsteadOfTogglingPause()
    {
        var messenger = new StrongReferenceMessenger();
        var album = new AlbumViewModel("Abbey Road", "The Beatles", messenger);
        var song1 = CreateDummyMedia("Come Together");
        var song2 = CreateDummyMedia("Something");
        song1.IsMediaActive = true;

        var activeState = new QueuePlaybackState(
            Guid.NewGuid(),
            QueueOrigin.ForSingleMedia(song1),
            song1,
            IsPlaying: true);

        messenger.Register<ContainerPlaybackStateTests, QueuePlaybackStateRequestMessage>(
            this,
            (_, m) => m.Reply(activeState));

        bool toggleSent = false;
        SetQueueMessage? queuedMessage = null;
        messenger.Register<ContainerPlaybackStateTests, TogglePlayPauseMessage>(
            this,
            (_, _) => toggleSent = true);
        messenger.Register<ContainerPlaybackStateTests, SetQueueMessage>(
            this,
            (_, m) => queuedMessage = m);

        try
        {
            messenger.SendQueueAndPlay(song1, new[] { song1, song2 }, origin: QueueOrigin.ForAlbum(album));

            await Assert.That(toggleSent).IsFalse();
            await Assert.That(queuedMessage).IsNotNull();
            await Assert.That(queuedMessage!.Value.Origin?.Matches(RecentItemKind.Album, album.AlbumKey) ?? false).IsTrue();
        }
        finally
        {
            messenger.UnregisterAll(this);
        }
    }
}

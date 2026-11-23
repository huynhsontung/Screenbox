using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Factories;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Data;

namespace Screenbox.Core.Tests.ViewModels;

public sealed class HomePageViewModelTests
{
    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private sealed class DummyCoordinator : IRecentMediaCoordinator
    {
        public Task LoadAsync(bool forceReload = false) => Task.CompletedTask;
        public Task LoadDetailsAsync() => Task.CompletedTask;
        public Task RemoveAsync(IRecentItem item) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }

    [Test]
    public async Task PlaySelectedNext_QueuesSelectedItemsInNaturalForwardOrder()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var settingsStorage = new MemorySettingsStorage();
        var generalSettings = new GeneralSettingsContext(settingsStorage);

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var track1 = CreateDummyMedia("Track 1");
        var track2 = CreateDummyMedia("Track 2");
        var track3 = CreateDummyMedia("Track 3");

        var album = new AlbumViewModel("Album 1", "Artist 1", messenger);
        album.RelatedSongs.Add(track1);
        album.RelatedSongs.Add(track2);

        recentContext.Recent = new IRecentItem[] { album, new RecentMediaItemViewModel(track3, messenger) };

        // Select both items via SelectionViewModel
        selection.IsSelectionModeActive = true;
        selection.SelectRange(new ItemIndexRange(0, 2));

        IReadOnlyList<QueueItemViewModel>? queuedItems = null;

        messenger.Register<QueueRequestMessage>(this, (_, msg) =>
        {
            msg.Reply(new QueueSession());
        });

        messenger.Register<SetQueueMessage>(this, (_, msg) =>
        {
            queuedItems = msg.Value.Items;
        });

        await vm.PlaySelectedNextCommand.ExecuteAsync(null);

        await Assert.That(queuedItems).IsNotNull();
        await Assert.That(queuedItems!.Count).IsEqualTo(3);
        await Assert.That(queuedItems[0].Media.Name).IsEqualTo("Track 1");
        await Assert.That(queuedItems[1].Media.Name).IsEqualTo("Track 2");
        await Assert.That(queuedItems[2].Media.Name).IsEqualTo("Track 3");
    }

    [Test]
    public async Task PlaySelected_MultipleItems_ClearsContainerOriginSoTracksRecordIndividually()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var settingsStorage = new MemorySettingsStorage();
        var generalSettings = new GeneralSettingsContext(settingsStorage);

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var track1 = new RecentMediaItemViewModel(CreateDummyMedia("Track 1"), messenger);
        var track2 = new RecentMediaItemViewModel(CreateDummyMedia("Track 2"), messenger);
        recentContext.Recent = new IRecentItem[] { track1, track2 };

        selection.IsSelectionModeActive = true;
        selection.SelectRange(new ItemIndexRange(0, 2));

        QueueSession? sentQueue = null;
        messenger.Register<SetQueueMessage>(this, (_, msg) => sentQueue = msg.Value);

        await vm.PlaySelectedCommand.ExecuteAsync(null);

        await Assert.That(sentQueue).IsNotNull();
        await Assert.That(sentQueue!.Items.Count).IsEqualTo(2);
        await Assert.That(sentQueue.Items[0].Origin).IsNull();
        await Assert.That(sentQueue.Items[1].Origin).IsNull();
    }

    [Test]
    public async Task ContextMedia_ResolvesAlbumArtistAndPlaylistViaPatternMatching()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var settingsStorage = new MemorySettingsStorage();
        var generalSettings = new GeneralSettingsContext(settingsStorage);

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var artist = new ArtistViewModel("Artist A", messenger);
        var album = new AlbumViewModel("Album A", "Artist A", messenger);
        var track = CreateDummyMedia("Track 1");
        track.Album = album;
        track.Artists = [artist];
        album.RelatedSongs.Add(track);
        artist.RelatedSongs.Add(track);

        vm.ContextMedia = new RecentMediaItemViewModel(track, messenger);
        await Assert.That(vm.ContextAlbum).IsSameReferenceAs(album);
        await Assert.That(vm.ContextArtist).IsSameReferenceAs(artist);
        await Assert.That(vm.ContextPlaylist).IsNull();

        vm.ContextMedia = album;
        await Assert.That(vm.ContextAlbum).IsSameReferenceAs(album);
        await Assert.That(vm.ContextArtist).IsSameReferenceAs(artist);

        vm.ContextMedia = artist;
        await Assert.That(vm.ContextAlbum).IsNull();
        await Assert.That(vm.ContextArtist).IsSameReferenceAs(artist);

        // Empty placeholders do not expose ContextAlbum/ContextArtist/ContextPlaylist
        vm.ContextMedia = new AlbumViewModel("Empty Album", "Artist", messenger);
        await Assert.That(vm.ContextAlbum).IsNull();
        vm.ContextMedia = new ArtistViewModel("Empty Artist", messenger);
        await Assert.That(vm.ContextArtist).IsNull();
        var emptyPlaylist = new PlaylistViewModel(null!, null!, NullLogger<PlaylistViewModel>.Instance, messenger);
        emptyPlaylist.LoadPlaceholder(Guid.NewGuid().ToString(), "Empty Playlist");
        vm.ContextMedia = emptyPlaylist;
        await Assert.That(vm.ContextPlaylist).IsNull();
    }

    [Test]
    public async Task Play_MediaViewModel_TogglesOnlyWhenPlayingIndividually()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var settingsStorage = new MemorySettingsStorage();
        var generalSettings = new GeneralSettingsContext(settingsStorage);

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var track = CreateDummyMedia("Track 1");
        var recentTrack = new RecentMediaItemViewModel(track, messenger);
        var album = new AlbumViewModel("Album A", "Artist A", messenger);

        int playMessages = 0;
        int toggleMessages = 0;
        messenger.Register<PlayMediaMessage>(this, (_, _) => playMessages++);
        messenger.Register<TogglePlayPauseMessage>(this, (_, _) => toggleMessages++);

        // Playing inside an album -> RecentMediaItemViewModel is inactive -> clicking card starts it standalone
        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(Guid.NewGuid(), QueueOrigin.ForAlbum(album), track, IsPlaying: true)));
        await Assert.That(recentTrack.IsMediaActive).IsFalse();
        await vm.PlayCommand.ExecuteAsync(recentTrack);
        await Assert.That(playMessages).IsEqualTo(1);
        await Assert.That(toggleMessages).IsEqualTo(0);

        // Playing individually -> RecentMediaItemViewModel is active -> clicking card toggles play/pause
        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(Guid.NewGuid(), null, track, IsPlaying: true)));
        await Assert.That(recentTrack.IsMediaActive).IsTrue();
        await Assert.That(recentTrack.IsPlaying).IsTrue();
        await vm.PlayCommand.ExecuteAsync(recentTrack);
        await Assert.That(toggleMessages).IsEqualTo(1);
    }

    [Test]
    public async Task Recent_SyncsOrderingAndRemovalsFromRecentContextSnapshots()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage());

        var track1 = new RecentMediaItemViewModel(CreateDummyMedia("Track 1"), messenger);
        var track2 = new RecentMediaItemViewModel(CreateDummyMedia("Track 2"), messenger);
        var track3 = new RecentMediaItemViewModel(CreateDummyMedia("Track 3"), messenger);

        // Pre-populated before HomePageViewModel construction
        recentContext.Recent = new IRecentItem[] { track1, track2 };

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        await Assert.That(vm.Recent.Count).IsEqualTo(2);
        await Assert.That(vm.Recent[0]).IsSameReferenceAs(track1);
        await Assert.That(vm.Recent[1]).IsSameReferenceAs(track2);

        // Reorder and add item via snapshot update
        recentContext.Recent = new IRecentItem[] { track3, track2, track1 };
        await Assert.That(vm.Recent.Count).IsEqualTo(3);
        await Assert.That(vm.Recent[0]).IsSameReferenceAs(track3);
        await Assert.That(vm.Recent[1]).IsSameReferenceAs(track2);
        await Assert.That(vm.Recent[2]).IsSameReferenceAs(track1);

        // Clear
        recentContext.Recent = Array.Empty<IRecentItem>();
        await Assert.That(vm.Recent).IsEmpty();
    }

    [Test]
    public async Task PlaceholderContainers_DisablePlayAndQueueCommands_ButAllowRemove()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage());

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var emptyAlbum = new AlbumViewModel("Album A", "Artist A", messenger);
        var emptyArtist = new ArtistViewModel("Artist A", messenger);
        var emptyPlaylist = new PlaylistViewModel(null!, null!, NullLogger<PlaylistViewModel>.Instance, messenger);
        emptyPlaylist.LoadPlaceholder(Guid.NewGuid().ToString(), "Playlist A");

        foreach (IRecentItem placeholder in new IRecentItem[] { emptyAlbum, emptyArtist, emptyPlaylist })
        {
            await Assert.That(vm.PlayCommand.CanExecute(placeholder)).IsFalse();
            await Assert.That(vm.PlayNextCommand.CanExecute(placeholder)).IsFalse();
            await Assert.That(vm.AddToQueueCommand.CanExecute(placeholder)).IsFalse();
            await Assert.That(vm.RemoveCommand.CanExecute(placeholder)).IsTrue();
        }
    }

    [Test]
    public async Task PlayCommand_PausedSingleMediaInIndividualQueue_ResumesInsteadOfReplacingQueue()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage());

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var track = CreateDummyMedia("Track 1");
        var album = new AlbumViewModel("Album A", "Artist A", messenger);
        QueuePlaybackState currentState = new(Guid.NewGuid(), null, track, IsPlaying: false);

        int toggleCount = 0;
        int playMediaCount = 0;
        messenger.Register<QueuePlaybackStateRequestMessage>(this, (_, msg) => msg.Reply(currentState));
        messenger.Register<TogglePlayPauseMessage>(this, (_, _) => toggleCount++);
        messenger.Register<PlayMediaMessage>(this, (_, _) => playMediaCount++);

        var recentTrack = new RecentMediaItemViewModel(track, messenger);

        // Paused in an individually tracked queue -> toggles play/pause instead of replacing queue
        await Assert.That(recentTrack.IsMediaActive).IsTrue();
        await Assert.That(recentTrack.IsPlaying).IsFalse();
        await vm.PlayCommand.ExecuteAsync(recentTrack);
        await Assert.That(toggleCount).IsEqualTo(1);
        await Assert.That(playMediaCount).IsEqualTo(0);

        // Paused inside a container queue (Album) -> starts track standalone via PlayMediaMessage
        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(Guid.NewGuid(), QueueOrigin.ForAlbum(album), track, IsPlaying: false)));
        await Assert.That(recentTrack.IsMediaActive).IsFalse();
        await vm.PlayCommand.ExecuteAsync(recentTrack);
        await Assert.That(toggleCount).IsEqualTo(1);
        await Assert.That(playMediaCount).IsEqualTo(1);
    }

    [Test]
    public async Task ShowRecent_WhenDisabled_HidesRecentItems()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage()) { ShowRecent = false };

        var track1 = new RecentMediaItemViewModel(CreateDummyMedia("Track 1"), messenger);
        recentContext.Recent = (IRecentItem[])[track1];

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        await Assert.That(vm.Recent).IsEmpty();

        // Context updates while ShowRecent is false stay hidden in the ViewModel
        recentContext.Recent = (IRecentItem[])[track1, new RecentMediaItemViewModel(CreateDummyMedia("Track 2"), messenger)];
        await Assert.That(vm.Recent).IsEmpty();

        // Enabling ShowRecent and calling OnLoaded surfaces the items
        generalSettings.ShowRecent = true;
        vm.OnLoaded();
        await Assert.That(vm.Recent.Count).IsEqualTo(2);
    }

    [Test]
    public async Task PlaySelected_MixedRecentOrigins_PreservesPerItemOrigins()
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var coordinator = new DummyCoordinator();
        var selection = new SelectionViewModel();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage());

        var vm = new HomePageViewModel(
            recentContext,
            coordinator,
            selection,
            mediaFactory,
            null!,
            generalSettings,
            null!,
            messenger);

        var album = new AlbumViewModel("Album A", "Artist A", messenger);
        var albumTrack = CreateDummyMedia("Album Track");
        album.RelatedSongs.Add(albumTrack);
        var singleTrack = CreateDummyMedia("Single Track");

        recentContext.Recent = (IRecentItem[])[album, new RecentMediaItemViewModel(singleTrack, messenger)];
        selection.IsSelectionModeActive = true;
        selection.SelectRange(new ItemIndexRange(0, 2));

        QueueSession? sentQueue = null;
        messenger.Register<SetQueueMessage>(this, (_, msg) => sentQueue = msg.Value);

        await vm.PlaySelectedCommand.ExecuteAsync(null);

        await Assert.That(sentQueue).IsNotNull();
        await Assert.That(sentQueue!.Items.Count).IsEqualTo(2);
        await Assert.That(sentQueue.Items[0].Media).IsSameReferenceAs(albumTrack);
        await Assert.That(sentQueue.Items[0].Origin).IsEqualTo(QueueOrigin.ForAlbum(album));
        await Assert.That(sentQueue.Items[1].Media).IsSameReferenceAs(singleTrack);
        await Assert.That(sentQueue.Items[1].Origin).IsNull();
    }
}

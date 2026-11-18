using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
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
        public Task LoadAsync() => Task.CompletedTask;
        public Task LoadDetailsAndThumbnailsAsync() => Task.CompletedTask;
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

        recentContext.Recent = new IRecentItem[] { album, track3 };

        // Select both items via SelectionViewModel
        selection.IsSelectionModeActive = true;
        selection.SelectRange(new ItemIndexRange(0, 2));

        IReadOnlyList<MediaViewModel>? queuedItems = null;

        messenger.Register<QueueRequestMessage>(this, (_, msg) =>
        {
            msg.Reply(new QueueSession(0, []));
        });

        messenger.Register<SetQueueMessage>(this, (_, msg) =>
        {
            queuedItems = msg.Value.GetCanonicalItems();
        });

        await vm.PlaySelectedNextCommand.ExecuteAsync(null);

        await Assert.That(queuedItems).IsNotNull();
        await Assert.That(queuedItems!.Count).IsEqualTo(3);
        await Assert.That(queuedItems[0].Name).IsEqualTo("Track 1");
        await Assert.That(queuedItems[1].Name).IsEqualTo("Track 2");
        await Assert.That(queuedItems[2].Name).IsEqualTo("Track 3");
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

        var track1 = CreateDummyMedia("Track 1");
        var track2 = CreateDummyMedia("Track 2");
        recentContext.Recent = new IRecentItem[] { track1, track2 };

        selection.IsSelectionModeActive = true;
        selection.SelectRange(new ItemIndexRange(0, 2));

        QueueSession? sentQueue = null;
        messenger.Register<SetQueueMessage>(this, (_, msg) => sentQueue = msg.Value);

        await vm.PlaySelectedCommand.ExecuteAsync(null);

        await Assert.That(sentQueue).IsNotNull();
        await Assert.That(sentQueue!.Items.Count).IsEqualTo(2);
        await Assert.That(sentQueue.Origin).IsNull();
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

        vm.ContextMedia = track;
        await Assert.That(vm.ContextAlbum).IsSameReferenceAs(album);
        await Assert.That(vm.ContextArtist).IsSameReferenceAs(artist);
        await Assert.That(vm.ContextPlaylist).IsNull();

        vm.ContextMedia = album;
        await Assert.That(vm.ContextAlbum).IsSameReferenceAs(album);
        await Assert.That(vm.ContextArtist).IsSameReferenceAs(artist);

        vm.ContextMedia = artist;
        await Assert.That(vm.ContextAlbum).IsNull();
        await Assert.That(vm.ContextArtist).IsSameReferenceAs(artist);
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
        track.IsPlaying = true;
        track.IsPlayingIndividually = false;

        int playMessages = 0;
        int toggleMessages = 0;
        messenger.Register<PlayMediaMessage>(this, (_, _) => playMessages++);
        messenger.Register<TogglePlayPauseMessage>(this, (_, _) => toggleMessages++);

        // Playing inside an album -> clicking card starts it standalone instead of pausing album
        await vm.PlayCommand.ExecuteAsync(track);
        await Assert.That(playMessages).IsEqualTo(1);
        await Assert.That(toggleMessages).IsEqualTo(0);

        // Playing individually -> clicking card toggles play/pause
        track.IsPlayingIndividually = true;
        await vm.PlayCommand.ExecuteAsync(track);
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

        var track1 = CreateDummyMedia("Track 1");
        var track2 = CreateDummyMedia("Track 2");
        var track3 = CreateDummyMedia("Track 3");

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
}

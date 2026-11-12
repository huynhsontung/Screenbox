using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.Coordinators;

public sealed class RecentMediaCoordinatorTests
{
    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private static (
        RecentMediaCoordinator Coordinator,
        RecentContext RecentContext,
        PlayQueueContext PlayQueueContext,
        LibraryContext LibraryContext,
        PlaylistsContext PlaylistsContext,
        DatabaseService DatabaseService) CreateCoordinator(string dbFolderPath)
    {
        var (c, r, pq, l, pl, db, _) = CreateCoordinator(dbFolderPath, isLoaded: true);
        return (c, r, pq, l, pl, db);
    }

    private static (
        RecentMediaCoordinator Coordinator,
        RecentContext RecentContext,
        PlayQueueContext PlayQueueContext,
        LibraryContext LibraryContext,
        PlaylistsContext PlaylistsContext,
        DatabaseService DatabaseService,
        GeneralSettingsContext GeneralSettings) CreateCoordinator(string dbFolderPath, bool isLoaded)
    {
        var recentContext = new RecentContext { IsLoaded = isLoaded };
        var playQueueContext = new PlayQueueContext();
        var libraryContext = new LibraryContext();
        var playlistsContext = new PlaylistsContext();
        var settingsStorage = new MemorySettingsStorage();
        var generalSettings = new GeneralSettingsContext(settingsStorage) { ShowRecent = true };
        var dbService = new DatabaseService(NullLogger<DatabaseService>.Instance)
        {
            DbFolderPath = dbFolderPath
        };
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var metadataService = new TestMetadataService();

        var coordinator = new RecentMediaCoordinator(
            recentContext,
            playQueueContext,
            libraryContext,
            playlistsContext,
            generalSettings,
            dbService,
            mediaFactory,
            metadataService,
            NullLogger<RecentMediaCoordinator>.Instance);

        return (coordinator, recentContext, playQueueContext, libraryContext, playlistsContext, dbService, generalSettings);
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_PromotesIdenticalMediaCollection_AndCreatesNewWhenItemsChange()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, playQueueContext, _, _, dbService) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");
        var single = CreateDummyMedia("single");

        var folderSession1 = new QueueSession(0, new[] { track1, track2 })
        {
            Origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA")
        };

        playQueueContext.ActiveSessionId = folderSession1.SessionId;
        playQueueContext.ActiveOrigin = folderSession1.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(folderSession1, isUpgrade: false);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.MediaCollection);
        await Assert.That(recentContext.Recent[0].IsQueueActive).IsTrue();

        // Play a single media item
        var singleSession = new QueueSession(single)
        {
            Origin = QueueOrigin.ForSingleMedia(single)
        };
        playQueueContext.ActiveSessionId = singleSession.SessionId;
        playQueueContext.ActiveOrigin = singleSession.Origin;
        playQueueContext.CurrentItem = single;
        await coordinator.HandleQueueSessionStartedAsync(singleSession, isUpgrade: false);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.SingleMedia);
        await Assert.That(recentContext.Recent[0].IsQueueActive).IsTrue();
        await Assert.That(recentContext.Recent[1].IsQueueActive).IsFalse();

        // Replay the exact same folder playlist -> promotes existing entry to index 0
        var folderSession2 = new QueueSession(0, new[] { track1, track2 })
        {
            Origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA")
        };
        playQueueContext.ActiveSessionId = folderSession2.SessionId;
        playQueueContext.ActiveOrigin = folderSession2.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(folderSession2, isUpgrade: false);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.MediaCollection);
        await Assert.That(recentContext.Recent[0].IsQueueActive).IsTrue();

        // Play the folder with an added track -> creates a new entry
        var track3 = CreateDummyMedia("track3");
        var modifiedFolderSession = new QueueSession(0, new[] { track1, track2, track3 })
        {
            Origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA")
        };
        playQueueContext.ActiveSessionId = modifiedFolderSession.SessionId;
        playQueueContext.ActiveOrigin = modifiedFolderSession.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(modifiedFolderSession, isUpgrade: false);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(3);
        await Assert.That(recentContext.Recent[0].ItemsCount).IsEqualTo(3d);
        await Assert.That(recentContext.Recent[1].ItemsCount).IsEqualTo(2d);
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_WithUpgrade_ReplacesInitialSingleMediaInPlace()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, playQueueContext, _, _, dbService) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");

        var initialSingle = new QueueSession(track1)
        {
            Origin = QueueOrigin.ForSingleMedia(track1)
        };
        playQueueContext.ActiveSessionId = initialSingle.SessionId;
        playQueueContext.ActiveOrigin = initialSingle.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(initialSingle, isUpgrade: false);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.SingleMedia);

        var upgradedPlaylist = new QueueSession(0, new[] { track1, track2 }, initialSingle)
        {
            Origin = QueueOrigin.ForMediaCollection("MyFolder", @"C:\MyFolder")
        };
        playQueueContext.ActiveOrigin = upgradedPlaylist.Origin;
        await coordinator.HandleQueueSessionStartedAsync(upgradedPlaylist, isUpgrade: true);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.MediaCollection);
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("MyFolder");
        await Assert.That(recentContext.Recent[0].ItemsCount).IsEqualTo(2d);
    }

    [Test]
    public async Task ActiveAndPlayingState_SyncsAcrossSessionAdvancesAndClears()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, playQueueContext, _, _, dbService) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");

        var folderSession = new QueueSession(0, new[] { track1, track2 })
        {
            Origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA")
        };
        playQueueContext.ActiveSessionId = folderSession.SessionId;
        playQueueContext.ActiveOrigin = folderSession.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(folderSession, isUpgrade: false);

        var recentFolder = recentContext.Recent[0];
        await Assert.That(recentFolder.IsQueueActive).IsTrue();
        await Assert.That(recentFolder.IsPlaying).IsFalse();

        // Start playing
        playQueueContext.IsPlaying = true;
        await Assert.That(recentFolder.IsPlaying).IsTrue();

        // Advance to track2 within the same session -> folder stays active & playing
        playQueueContext.CurrentItem = track2;
        await Assert.That(recentFolder.IsQueueActive).IsTrue();
        await Assert.That(recentFolder.IsPlaying).IsTrue();

        // Pause playback -> folder stays active, but IsPlaying is false
        playQueueContext.IsPlaying = false;
        await Assert.That(recentFolder.IsQueueActive).IsTrue();
        await Assert.That(recentFolder.IsPlaying).IsFalse();

        // Clear queue -> folder becomes inactive and not playing
        playQueueContext.ActiveSessionId = Guid.Empty;
        playQueueContext.ActiveOrigin = null;
        playQueueContext.CurrentItem = null;
        await Assert.That(recentFolder.IsQueueActive).IsFalse();
        await Assert.That(recentFolder.IsPlaying).IsFalse();
    }

    [Test]
    public async Task ShowRecent_Toggle_ClearsAndReloadsFromDatabase()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, playQueueContext, _, _, dbService, generalSettings) =
            CreateCoordinator(fixture.DirectoryPath, isLoaded: false);
        await dbService.InitializeAsync();

        // 1. Initial queue session recorded
        var track = CreateDummyMedia("song1");
        var session = new QueueSession(track) { Origin = QueueOrigin.ForSingleMedia(track) };
        playQueueContext.ActiveSessionId = session.SessionId;
        playQueueContext.ActiveOrigin = session.Origin;
        playQueueContext.CurrentItem = track;
        await coordinator.HandleQueueSessionStartedAsync(session, isUpgrade: false);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.IsLoaded).IsTrue();

        // 2. Toggle ShowRecent to false -> clears in-memory collection and marks IsLoaded false
        generalSettings.ShowRecent = false;
        await Assert.That(recentContext.Recent).IsEmpty();
        await Assert.That(recentContext.IsLoaded).IsFalse();

        // 3. Toggle ShowRecent back to true -> reloads the entry from SQLite
        generalSettings.ShowRecent = true;
        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.IsLoaded).IsTrue();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("song1");
    }

    [Test]
    public async Task LoadAsync_WhenInvokedConcurrently_CoalescesIntoSingleLoad()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, _, dbService, _) =
            CreateCoordinator(fixture.DirectoryPath, isLoaded: false);
        await dbService.InitializeAsync();

        // Seed a recent entry in SQLite
        var dto = new RecentEntryDto
        {
            Id = Guid.NewGuid().ToString(),
            Kind = RecentItemKind.SingleMedia,
            ContentHash = "hash1",
            Title = "SavedTrack",
            SourceId = "https://example.com/SavedTrack.mp3",
            LastPlayed = DateTimeOffset.UtcNow,
            Items = [new RawMediaRecordDto { Path = "https://example.com/SavedTrack.mp3", Title = "SavedTrack" }]
        };
        await dbService.UpsertRecentEntryAsync(dto);

        // Run 5 concurrent loads
        await Task.WhenAll(
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync());

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.IsLoaded).IsTrue();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("SavedTrack");
    }
}

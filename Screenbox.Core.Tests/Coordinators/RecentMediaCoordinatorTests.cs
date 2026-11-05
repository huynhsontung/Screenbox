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
        var recentContext = new RecentContext { IsLoaded = true };
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

        return (coordinator, recentContext, playQueueContext, libraryContext, playlistsContext, dbService);
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_PromotesIdenticalAdHocPlaylist_AndCreatesNewWhenItemsChange()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, playQueueContext, _, _, dbService) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");
        var single = CreateDummyMedia("single");

        var folderSession1 = new Playlist(0, new[] { track1, track2 })
        {
            Origin = PlaylistOrigin.ForAdHocPlaylist("FolderA", @"C:\FolderA")
        };

        playQueueContext.ActiveSessionId = folderSession1.SessionId;
        playQueueContext.ActiveOrigin = folderSession1.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(folderSession1, isUpgrade: false);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.AdHocPlaylist);
        await Assert.That(recentContext.Recent[0].IsQueueActive).IsTrue();

        // Play a single media item
        var singleSession = new Playlist(single)
        {
            Origin = PlaylistOrigin.ForSingleMedia(single)
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
        var folderSession2 = new Playlist(0, new[] { track1, track2 })
        {
            Origin = PlaylistOrigin.ForAdHocPlaylist("FolderA", @"C:\FolderA")
        };
        playQueueContext.ActiveSessionId = folderSession2.SessionId;
        playQueueContext.ActiveOrigin = folderSession2.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(folderSession2, isUpgrade: false);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.AdHocPlaylist);
        await Assert.That(recentContext.Recent[0].IsQueueActive).IsTrue();

        // Play the folder with an added track -> creates a new entry
        var track3 = CreateDummyMedia("track3");
        var modifiedFolderSession = new Playlist(0, new[] { track1, track2, track3 })
        {
            Origin = PlaylistOrigin.ForAdHocPlaylist("FolderA", @"C:\FolderA")
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

        var initialSingle = new Playlist(track1)
        {
            Origin = PlaylistOrigin.ForSingleMedia(track1)
        };
        playQueueContext.ActiveSessionId = initialSingle.SessionId;
        playQueueContext.ActiveOrigin = initialSingle.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(initialSingle, isUpgrade: false);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.SingleMedia);

        var upgradedPlaylist = new Playlist(0, new[] { track1, track2 }, initialSingle)
        {
            Origin = PlaylistOrigin.ForAdHocPlaylist("MyFolder", @"C:\MyFolder")
        };
        playQueueContext.ActiveOrigin = upgradedPlaylist.Origin;
        await coordinator.HandleQueueSessionStartedAsync(upgradedPlaylist, isUpgrade: true);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Kind).IsEqualTo(RecentItemKind.AdHocPlaylist);
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

        var folderSession = new Playlist(0, new[] { track1, track2 })
        {
            Origin = PlaylistOrigin.ForAdHocPlaylist("FolderA", @"C:\FolderA")
        };
        playQueueContext.ActiveSessionId = folderSession.SessionId;
        playQueueContext.ActiveOrigin = folderSession.Origin;
        playQueueContext.CurrentItem = track1;
        await coordinator.HandleQueueSessionStartedAsync(folderSession, isUpgrade: false);

        var recentFolder = recentContext.Recent[0];
        await Assert.That(recentFolder.IsQueueActive).IsTrue();
        await Assert.That(recentFolder.IsPlaying).IsFalse();

        // Start playing track1
        track1.IsPlaying = true;
        await Assert.That(recentFolder.IsPlaying).IsTrue();

        // Advance to track2 within the same session -> folder stays active & playing
        track1.IsPlaying = false;
        track2.IsPlaying = true;
        playQueueContext.CurrentItem = track2;
        await Assert.That(recentFolder.IsQueueActive).IsTrue();
        await Assert.That(recentFolder.IsPlaying).IsTrue();

        // Clear queue -> folder becomes inactive and not playing
        track2.IsPlaying = false;
        playQueueContext.ActiveSessionId = Guid.Empty;
        playQueueContext.ActiveOrigin = null;
        playQueueContext.CurrentItem = null;
        await Assert.That(recentFolder.IsQueueActive).IsFalse();
        await Assert.That(recentFolder.IsPlaying).IsFalse();
    }
}

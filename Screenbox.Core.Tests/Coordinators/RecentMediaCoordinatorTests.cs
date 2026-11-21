using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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
    private sealed class FlakyDatabaseService : IDatabaseService
    {
        private readonly TaskCompletionSource<List<RecentEntryDto>>? _blockedLoadTcs;
        private readonly Func<int, Task<List<RecentEntryDto>>>? _loadHandler;
        private int _loadCalls;
        private readonly bool _failFirstLoad;

        public int LoadCallCount => _loadCalls;

        public List<RecentEntryDto> Entries { get; } = [];

        public FlakyDatabaseService(
            bool failFirstLoad = false,
            TaskCompletionSource<List<RecentEntryDto>>? blockedLoadTcs = null,
            Func<int, Task<List<RecentEntryDto>>>? loadHandler = null)
        {
            _failFirstLoad = failFirstLoad;
            _blockedLoadTcs = blockedLoadTcs;
            _loadHandler = loadHandler;
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task<RawCacheLoadResultDto> LoadLibraryCacheAsync(MediaPlaybackType mediaType) => Task.FromResult(new RawCacheLoadResultDto());
        public Task SaveMusicCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<MusicCacheRecordDto> records) => Task.CompletedTask;
        public Task SaveVideoCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<VideoCacheRecordDto> records) => Task.CompletedTask;
        public Task ReplacePlaybackProgressAsync(IReadOnlyList<MediaPlaybackProgress> snapshot) => Task.CompletedTask;
        public Task<List<MediaPlaybackProgress>> LoadPlaybackProgressAsync() => Task.FromResult(new List<MediaPlaybackProgress>());
        public Task SavePlaylistAsync(PlaylistRecordDto playlist) => Task.CompletedTask;
        public Task<PlaylistRecordDto?> LoadPlaylistAsync(string id) => Task.FromResult<PlaylistRecordDto?>(null);
        public Task<List<PlaylistRecordDto>> ListPlaylistsAsync() => Task.FromResult(new List<PlaylistRecordDto>());
        public Task DeletePlaylistAsync(string id) => Task.CompletedTask;

        public Task<List<RecentEntryDto>> LoadRecentEntriesAsync(int maxEntries = 100)
        {
            _loadCalls++;
            if (_loadHandler is not null)
            {
                return _loadHandler(_loadCalls);
            }

            if (_failFirstLoad && _loadCalls == 1)
            {
                throw new InvalidOperationException("Simulated DB failure");
            }

            if (_blockedLoadTcs is not null)
            {
                return _blockedLoadTcs.Task;
            }

            return Task.FromResult(new List<RecentEntryDto>(Entries));
        }

        public Task UpsertRecentEntryAsync(RecentEntryDto entry, string? replaceEntryId = null, int maxEntries = 100) => Task.CompletedTask;
        public Task DeleteRecentEntryAsync(string id) => Task.CompletedTask;
        public Task ClearRecentEntriesAsync()
        {
            Entries.Clear();
            return Task.CompletedTask;
        }
    }

    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private static (
        RecentMediaCoordinator Coordinator,
        RecentContext RecentContext,
        LibraryContext LibraryContext,
        PlaylistsContext PlaylistsContext,
        DatabaseService DatabaseService,
        GeneralSettingsContext GeneralSettings) CreateCoordinator(string dbFolderPath)
    {
        var dbService = new DatabaseService(NullLogger<DatabaseService>.Instance)
        {
            DbFolderPath = dbFolderPath
        };
        var (coordinator, recentContext, libraryContext, playlistsContext, generalSettings) = CreateCoordinatorWithDb(dbService);
        return (coordinator, recentContext, libraryContext, playlistsContext, dbService, generalSettings);
    }

    private static (
        RecentMediaCoordinator Coordinator,
        RecentContext RecentContext,
        LibraryContext LibraryContext,
        PlaylistsContext PlaylistsContext,
        GeneralSettingsContext GeneralSettings) CreateCoordinatorWithDb(IDatabaseService dbService)
    {
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var libraryContext = new LibraryContext();
        var playlistsContext = new PlaylistsContext();
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage()) { ShowRecent = true };
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);

        var coordinator = new RecentMediaCoordinator(
            recentContext,
            libraryContext,
            playlistsContext,
            generalSettings,
            dbService,
            mediaFactory,
            new TestMetadataService(),
            null!,
            NullLogger<RecentMediaCoordinator>.Instance,
            messenger,
            () => new PlaylistViewModel(null!, null!, NullLogger<PlaylistViewModel>.Instance, messenger));

        return (coordinator, recentContext, libraryContext, playlistsContext, generalSettings);
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_PromotesFolderShortcut_AndTracksLooseFilesIndividually()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track3 = CreateDummyMedia("track3");
        var single = CreateDummyMedia("single");

        await coordinator.HandleQueueSessionStartedAsync(
            QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA", token: "tok_a"));

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsTypeOf<MediaCollectionViewModel>();
        await Assert.That(((MediaCollectionViewModel)recentContext.Recent[0]).Token).IsEqualTo("tok_a");

        // Play a single media item -> moves to top
        await coordinator.HandleQueueSessionStartedAsync(
            QueueOrigin.ForSingleMedia(single),
            single);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(single);

        // Replay the same folder -> promotes the existing shortcut and refreshes token
        await coordinator.HandleQueueSessionStartedAsync(
            QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA", token: "tok_b"));

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0]).IsTypeOf<MediaCollectionViewModel>();
        await Assert.That(((MediaCollectionViewModel)recentContext.Recent[0]).Token).IsEqualTo("tok_b");

        // Collection origin with empty source path tracks the active item individually as SingleMedia
        await coordinator.HandleQueueSessionStartedAsync(
            QueueOrigin.ForMediaCollection("Loose", null),
            track3);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(3);
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(track3);
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_WithUpgrade_ReplacesInitialSingleMediaInPlace()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track1 = CreateDummyMedia("track1");

        await coordinator.HandleQueueSessionStartedAsync(
            QueueOrigin.ForSingleMedia(track1),
            track1);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(track1);

        await coordinator.HandleQueueSessionStartedAsync(
            QueueOrigin.ForMediaCollection("MyFolder", @"C:\MyFolder"),
            replacedEntryId: QueueOrigin.ForSingleMedia(track1).GetEntryId());

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsTypeOf<MediaCollectionViewModel>();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("MyFolder");
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_WithUpgradeAfterSwitchingMedia_ReplacesOriginalTrackNotNewTrack()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");

        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(track1), track1);
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(track2), track2);

        // Delayed upgrade for track1 arrives after user already switched to track2
        await coordinator.HandleQueueSessionStartedAsync(
            QueueOrigin.ForMediaCollection("MyFolder", @"C:\MyFolder"),
            replacedEntryId: QueueOrigin.ForSingleMedia(track1).GetEntryId());

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0]).IsTypeOf<MediaCollectionViewModel>();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("MyFolder");
        await Assert.That(recentContext.Recent[1]).IsSameReferenceAs(track2);
    }

    [Test]
    public async Task ReceiveMusicLibraryChanged_SwapsPlaceholderAlbumWhenMusicLibraryLoads()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, libraryContext, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var albumKey = AlbumViewModelFactory.GetAlbumKey("Abbey Road", "The Beatles");
        await dbService.UpsertRecentEntryAsync(new RecentEntryDto
        {
            Id = QueueOrigin.GetEntryId(RecentItemKind.Album, albumKey),
            Kind = RecentItemKind.Album,
            SourceId = albumKey,
            Title = "Abbey Road",
            Subtitle = "The Beatles",
            LastPlayed = DateTimeOffset.UtcNow
        });

        await coordinator.LoadAsync();
        await Assert.That(recentContext.Recent).HasSingleItem();
        var placeholder = (AlbumViewModel)recentContext.Recent[0];
        await Assert.That(placeholder.RelatedSongs).IsEmpty();
        await Assert.That(placeholder.HasSongs).IsFalse();

        // Populate live library
        var liveAlbum = new AlbumViewModel("Abbey Road", "The Beatles");
        var song = CreateDummyMedia("Come Together");
        liveAlbum.RelatedSongs.Add(song);
        var albums = new Dictionary<string, AlbumViewModel> { [albumKey] = liveAlbum };
        var musicLibrary = new MusicLibrary(
            new[] { song },
            albums,
            new Dictionary<string, ArtistViewModel>(),
            Array.Empty<string>(),
            new AlbumViewModel(),
            new ArtistViewModel());

        // Intermediate progress updates while IsLoadingMusic == true do not trigger reload
        libraryContext.IsLoadingMusic = true;
        libraryContext.Music = musicLibrary;
        coordinator.Receive(new PropertyChangedMessage<MusicLibrary>(libraryContext, nameof(LibraryContext.Music), MusicLibrary.Empty, musicLibrary));
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(placeholder);

        // Final MusicLibrary completion swaps placeholder with live album
        libraryContext.IsLoadingMusic = false;
        coordinator.Receive(new PropertyChangedMessage<MusicLibrary>(libraryContext, nameof(LibraryContext.Music), MusicLibrary.Empty, musicLibrary));
        // Allow async void Receive continuation to complete
        await Task.Yield();

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(liveAlbum);
        await Assert.That(((AlbumViewModel)recentContext.Recent[0]).RelatedSongs).HasSingleItem();
    }

    [Test]
    public async Task LoadAsync_ForceReload_ReplacesPlaceholdersAndReusesMediaCollections()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, playlistsContext, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        string playlistId = Guid.NewGuid().ToString();
        await dbService.UpsertRecentEntryAsync(new RecentEntryDto
        {
            Id = QueueOrigin.GetEntryId(RecentItemKind.Playlist, playlistId),
            Kind = RecentItemKind.Playlist,
            SourceId = playlistId,
            Title = "Road Trip",
            LastPlayed = DateTimeOffset.UtcNow
        });

        await dbService.UpsertRecentEntryAsync(new RecentEntryDto
        {
            Id = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"C:\MusicFolder"),
            Kind = RecentItemKind.MediaCollection,
            SourceId = @"C:\MusicFolder",
            Title = "MusicFolder",
            Token = "tok_1",
            LastPlayed = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        // Initial load creates non-playable playlist placeholder and MediaCollection
        await coordinator.LoadAsync();
        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        var placeholderPlaylist = (PlaylistViewModel)recentContext.Recent[0];
        var initialCollection = (MediaCollectionViewModel)recentContext.Recent[1];
        await Assert.That(placeholderPlaylist.HasItems).IsFalse();

        // Simulate saved playlists finishing load after startup
        var livePlaylist = new PlaylistViewModel(null!, null!, NullLogger<PlaylistViewModel>.Instance, new StrongReferenceMessenger());
        livePlaylist.LoadPlaceholder(playlistId, "Road Trip");
        livePlaylist.Items.Add(CreateDummyMedia("Song 1"));
        playlistsContext.Playlists.Add(livePlaylist);

        await coordinator.LoadAsync(forceReload: true);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(livePlaylist);
        await Assert.That(((PlaylistViewModel)recentContext.Recent[0]).HasItems).IsTrue();
        await Assert.That(recentContext.Recent[1]).IsSameReferenceAs(initialCollection);
    }

    [Test]
    public async Task ShowRecent_Toggle_ClearsAndReloadsFromDatabase()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, generalSettings) =
            CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        // 1. Initial queue session recorded
        var track = CreateDummyMedia("song1");
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(track), track);

        await Assert.That(recentContext.Recent).HasSingleItem();

        // 2. Toggle ShowRecent to false -> clears in-memory collection
        generalSettings.ShowRecent = false;
        await Assert.That(recentContext.Recent).IsEmpty();

        // 3. Toggle ShowRecent back to true -> reloads the entry from SQLite
        generalSettings.ShowRecent = true;
        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("song1");
    }

    [Test]
    public async Task LoadAsync_WhenInvokedConcurrently_CoalescesIntoSingleLoad()
    {
        var tcs = new TaskCompletionSource<List<RecentEntryDto>>();
        var db = new FlakyDatabaseService(blockedLoadTcs: tcs);
        var (coordinator, recentContext, _, _, _) = CreateCoordinatorWithDb(db);

        var tasks = Task.WhenAll(
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync());

        tcs.SetResult([
            new RecentEntryDto
            {
                Id = QueueOrigin.GetEntryId(RecentItemKind.SingleMedia, "https://example.com/SavedTrack.mp3"),
                Kind = RecentItemKind.SingleMedia,
                Title = "SavedTrack",
                SourceId = "https://example.com/SavedTrack.mp3",
                LastPlayed = DateTimeOffset.UtcNow
            }
        ]);

        await tasks;

        await Assert.That(db.LoadCallCount).IsEqualTo(1);
        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("SavedTrack");

        // Subsequent calls remain idempotent once loaded
        await coordinator.LoadAsync();
        await Assert.That(db.LoadCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task LoadAsync_WhenFirstLoadFailsSynchronously_RetriesOnSubsequentCall()
    {
        var flakyDb = new FlakyDatabaseService(failFirstLoad: true);
        flakyDb.Entries.Add(new RecentEntryDto
        {
            Id = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"C:\Music"),
            Kind = RecentItemKind.MediaCollection,
            SourceId = @"C:\Music",
            Title = "Music",
            LastPlayed = DateTimeOffset.UtcNow
        });

        var (coordinator, recentContext, _, _, _) = CreateCoordinatorWithDb(flakyDb);

        await coordinator.LoadAsync();
        await Assert.That(flakyDb.LoadCallCount).IsEqualTo(1);
        await Assert.That(recentContext.Recent).IsEmpty();

        // Retry succeeds
        await coordinator.LoadAsync();
        await Assert.That(flakyDb.LoadCallCount).IsEqualTo(2);
        await Assert.That(recentContext.Recent).HasSingleItem();
    }

    [Test]
    public async Task LoadAsync_WhenClearedWhileInFlight_DoesNotRestoreStaleCards()
    {
        var tcs = new TaskCompletionSource<List<RecentEntryDto>>();
        var blockedDb = new FlakyDatabaseService(blockedLoadTcs: tcs);
        var (coordinator, recentContext, _, _, _) = CreateCoordinatorWithDb(blockedDb);

        Task pendingLoad = coordinator.LoadAsync();
        await coordinator.ClearAsync();

        tcs.SetResult([
            new RecentEntryDto
            {
                Id = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"C:\Stale"),
                Kind = RecentItemKind.MediaCollection,
                SourceId = @"C:\Stale",
                Title = "Stale",
                LastPlayed = DateTimeOffset.UtcNow
            }
        ]);

        await pendingLoad;
        await Assert.That(recentContext.Recent).IsEmpty();
    }

    [Test]
    public async Task LoadAsync_WhenShowRecentToggledRapidly_CancelsStaleLoadAndKeepsNewerResult()
    {
        var staleTcs = new TaskCompletionSource<List<RecentEntryDto>>();
        var db = new FlakyDatabaseService(loadHandler: call =>
        {
            if (call == 1)
            {
                return staleTcs.Task;
            }

            return Task.FromResult(new List<RecentEntryDto>
            {
                new()
                {
                    Id = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"C:\Fresh"),
                    Kind = RecentItemKind.MediaCollection,
                    SourceId = @"C:\Fresh",
                    Title = "Fresh",
                    LastPlayed = DateTimeOffset.UtcNow
                }
            });
        });

        var (coordinator, recentContext, _, _, generalSettings) = CreateCoordinatorWithDb(db);

        Task staleLoad = coordinator.LoadAsync();
        generalSettings.ShowRecent = false;
        generalSettings.ShowRecent = true;

        staleTcs.SetResult([
            new RecentEntryDto
            {
                Id = QueueOrigin.GetEntryId(RecentItemKind.MediaCollection, @"C:\Stale"),
                Kind = RecentItemKind.MediaCollection,
                SourceId = @"C:\Stale",
                Title = "Stale",
                LastPlayed = DateTimeOffset.UtcNow
            }
        ]);

        await staleLoad;
        await coordinator.LoadAsync();

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("Fresh");
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_WhenClonedSingleMediaPlays_ReplacesRetainedInstance()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var original = CreateDummyMedia("song1");
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(original), original);
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(original);

        // Simulate SendPlayNext / SendAddToQueue / InsertNext cloning the MediaViewModel
        var clone = new MediaViewModel(original) { IsPlayingIndividually = true };
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(clone), clone);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(clone);
        await Assert.That(((MediaViewModel)recentContext.Recent[0]).IsPlayingIndividually).IsTrue();
    }
}

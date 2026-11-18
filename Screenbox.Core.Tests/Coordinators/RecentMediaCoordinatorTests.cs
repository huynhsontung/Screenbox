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
        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
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
            libraryContext,
            playlistsContext,
            generalSettings,
            dbService,
            mediaFactory,
            metadataService,
            null!,
            NullLogger<RecentMediaCoordinator>.Instance,
            messenger);

        return (coordinator, recentContext, libraryContext, playlistsContext, dbService, generalSettings);
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_PromotesFolderShortcut_AndTracksLooseFilesIndividually()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");
        var single = CreateDummyMedia("single");

        var folderSession1 = new QueueSession(0, new[] { track1, track2 })
        {
            Origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA", token: "tok_a")
        };

        await coordinator.HandleQueueSessionStartedAsync(folderSession1, isUpgrade: false);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsTypeOf<MediaCollectionViewModel>();
        await Assert.That(((MediaCollectionViewModel)recentContext.Recent[0]).Token).IsEqualTo("tok_a");

        // Play a single media item -> moves to top
        var singleSession = new QueueSession(single)
        {
            Origin = QueueOrigin.ForSingleMedia(single)
        };
        await coordinator.HandleQueueSessionStartedAsync(singleSession, isUpgrade: false);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(single);

        // Replay the same folder (even with extra tracks on disk) -> promotes the existing single shortcut
        var track3 = CreateDummyMedia("track3");
        var folderSession2 = new QueueSession(0, new[] { track1, track2, track3 })
        {
            Origin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA", token: "tok_b")
        };
        await coordinator.HandleQueueSessionStartedAsync(folderSession2, isUpgrade: false);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0]).IsTypeOf<MediaCollectionViewModel>();
        await Assert.That(((MediaCollectionViewModel)recentContext.Recent[0]).Token).IsEqualTo("tok_b");

        // Loose multi-file queue (Origin == null) tracks the active item individually
        var looseSession = new QueueSession(1, new[] { track1, track3 });
        await coordinator.HandleQueueSessionStartedAsync(looseSession, isUpgrade: false);

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
        var track2 = CreateDummyMedia("track2");

        var initialSingle = new QueueSession(track1)
        {
            Origin = QueueOrigin.ForSingleMedia(track1)
        };
        await coordinator.HandleQueueSessionStartedAsync(initialSingle, isUpgrade: false);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(track1);

        var upgradedPlaylist = new QueueSession(0, new[] { track1, track2 }, initialSingle)
        {
            Origin = QueueOrigin.ForMediaCollection("MyFolder", @"C:\MyFolder")
        };
        await coordinator.HandleQueueSessionStartedAsync(upgradedPlaylist, isUpgrade: true);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0]).IsTypeOf<MediaCollectionViewModel>();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("MyFolder");
    }

    [Test]
    public async Task RelinkLibraryItems_SwapsPlaceholderAlbumAndArtistWhenMusicLibraryLoads()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, libraryContext, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var albumKey = AlbumViewModelFactory.GetAlbumKey("Abbey Road", "The Beatles");
        await dbService.UpsertRecentEntryAsync(new RecentEntryDto
        {
            Id = "entry_album",
            Kind = RecentItemKind.Album,
            SourceId = albumKey,
            Title = "Abbey Road",
            Subtitle = "The Beatles",
            ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.Album, albumKey),
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
            Id = "entry_playlist",
            Kind = RecentItemKind.Playlist,
            SourceId = playlistId,
            Title = "Road Trip",
            ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.Playlist, playlistId),
            LastPlayed = DateTimeOffset.UtcNow
        });

        await dbService.UpsertRecentEntryAsync(new RecentEntryDto
        {
            Id = "entry_folder",
            Kind = RecentItemKind.MediaCollection,
            SourceId = @"C:\MusicFolder",
            Title = "MusicFolder",
            Token = "tok_1",
            ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.MediaCollection, @"C:\MusicFolder"),
            LastPlayed = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        // Initial load creates non-playable playlist placeholder and MediaCollection
        await coordinator.LoadAsync();
        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        var placeholderPlaylist = (PlaylistViewModel)recentContext.Recent[0];
        var initialCollection = (MediaCollectionViewModel)recentContext.Recent[1];
        await Assert.That(placeholderPlaylist.HasItems).IsFalse();

        // Simulate saved playlists finishing load after startup
        var livePlaylist = new PlaylistViewModel(playlistId, "Road Trip", new StrongReferenceMessenger());
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
        var session = new QueueSession(track) { Origin = QueueOrigin.ForSingleMedia(track) };
        await coordinator.HandleQueueSessionStartedAsync(session, isUpgrade: false);

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

        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var libraryContext = new LibraryContext();
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage()) { ShowRecent = true };
        var coordinator = new RecentMediaCoordinator(
            recentContext,
            libraryContext,
            new PlaylistsContext(),
            generalSettings,
            db,
            new MediaViewModelFactory(new MediaSourceFactory(), libraryContext),
            new TestMetadataService(),
            null!,
            NullLogger<RecentMediaCoordinator>.Instance,
            messenger);

        var tasks = Task.WhenAll(
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync(),
            coordinator.LoadAsync());

        tcs.SetResult([
            new RecentEntryDto
            {
                Id = Guid.NewGuid().ToString(),
                Kind = RecentItemKind.SingleMedia,
                ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.SingleMedia, "https://example.com/SavedTrack.mp3"),
                Title = "SavedTrack",
                SourceId = "https://example.com/SavedTrack.mp3",
                LastPlayed = DateTimeOffset.UtcNow,
                Items = [new RawMediaRecordDto { Path = "https://example.com/SavedTrack.mp3", Title = "SavedTrack" }]
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
            Id = "e1",
            Kind = RecentItemKind.MediaCollection,
            SourceId = @"C:\Music",
            Title = "Music",
            ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.MediaCollection, @"C:\Music"),
            LastPlayed = DateTimeOffset.UtcNow
        });

        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var libraryContext = new LibraryContext();
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage()) { ShowRecent = true };
        var coordinator = new RecentMediaCoordinator(
            recentContext,
            libraryContext,
            new PlaylistsContext(),
            generalSettings,
            flakyDb,
            new MediaViewModelFactory(new MediaSourceFactory(), libraryContext),
            new TestMetadataService(),
            null!,
            NullLogger<RecentMediaCoordinator>.Instance,
            messenger);

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

        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var libraryContext = new LibraryContext();
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage()) { ShowRecent = true };
        var coordinator = new RecentMediaCoordinator(
            recentContext,
            libraryContext,
            new PlaylistsContext(),
            generalSettings,
            blockedDb,
            new MediaViewModelFactory(new MediaSourceFactory(), libraryContext),
            new TestMetadataService(),
            null!,
            NullLogger<RecentMediaCoordinator>.Instance,
            messenger);

        Task pendingLoad = coordinator.LoadAsync();
        await coordinator.ClearAsync();

        tcs.SetResult([
            new RecentEntryDto
            {
                Id = "stale",
                Kind = RecentItemKind.MediaCollection,
                SourceId = @"C:\Stale",
                Title = "Stale",
                ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.MediaCollection, @"C:\Stale"),
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
                    Id = "fresh",
                    Kind = RecentItemKind.MediaCollection,
                    SourceId = @"C:\Fresh",
                    Title = "Fresh",
                    ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.MediaCollection, @"C:\Fresh"),
                    LastPlayed = DateTimeOffset.UtcNow
                }
            });
        });

        var messenger = new StrongReferenceMessenger();
        var recentContext = new RecentContext(messenger);
        var libraryContext = new LibraryContext();
        var generalSettings = new GeneralSettingsContext(new MemorySettingsStorage()) { ShowRecent = true };
        var coordinator = new RecentMediaCoordinator(
            recentContext,
            libraryContext,
            new PlaylistsContext(),
            generalSettings,
            db,
            new MediaViewModelFactory(new MediaSourceFactory(), libraryContext),
            new TestMetadataService(),
            null!,
            NullLogger<RecentMediaCoordinator>.Instance,
            messenger);

        Task staleLoad = coordinator.LoadAsync();
        generalSettings.ShowRecent = false;
        generalSettings.ShowRecent = true;

        staleTcs.SetResult([
            new RecentEntryDto
            {
                Id = "stale",
                Kind = RecentItemKind.MediaCollection,
                SourceId = @"C:\Stale",
                Title = "Stale",
                ContentHash = QueueOrigin.ComputeContentHash(RecentItemKind.MediaCollection, @"C:\Stale"),
                LastPlayed = DateTimeOffset.UtcNow
            }
        ]);

        await staleLoad;
        await coordinator.LoadAsync();

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("Fresh");
    }
}

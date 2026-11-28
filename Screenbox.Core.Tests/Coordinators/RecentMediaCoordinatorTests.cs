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
        var filesService = new FilesService(NullLogger<FilesService>.Instance);
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(filesService), libraryContext);

        var coordinator = new RecentMediaCoordinator(
            recentContext,
            libraryContext,
            playlistsContext,
            generalSettings,
            dbService,
            filesService,
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
        await Assert.That(((RecentMediaItemViewModel)recentContext.Recent[0]).Media).IsSameReferenceAs(single);

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
        await Assert.That(((RecentMediaItemViewModel)recentContext.Recent[0]).Media).IsSameReferenceAs(track3);
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
        await Assert.That(((RecentMediaItemViewModel)recentContext.Recent[0]).Media).IsSameReferenceAs(track1);

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
        await Assert.That(((RecentMediaItemViewModel)recentContext.Recent[1]).Media).IsSameReferenceAs(track2);
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
        livePlaylist.Items.Add(new PlaylistItemViewModel(CreateDummyMedia("Song 1")));
        playlistsContext.Playlists.Add(livePlaylist);

        await coordinator.LoadAsync(forceReload: true);

        await Assert.That(recentContext.Recent.Count).IsEqualTo(2);
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(livePlaylist);
        await Assert.That(((PlaylistViewModel)recentContext.Recent[0]).HasItems).IsTrue();
        await Assert.That(recentContext.Recent[1]).IsSameReferenceAs(initialCollection);
    }

    [Test]
    public async Task ShowRecent_WhenDisabled_SkipsRecordingAndPreservesExistingEntries()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, generalSettings) =
            CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        // 1. Initial queue session recorded while ShowRecent is enabled
        var track1 = CreateDummyMedia("song1");
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(track1), track1);
        await Assert.That(recentContext.Recent).HasSingleItem();

        // 2. Disable ShowRecent -> new playback is not recorded and existing history is untouched
        generalSettings.ShowRecent = false;
        var track2 = CreateDummyMedia("song2");
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(track2), track2);

        var persisted = await dbService.LoadRecentEntriesAsync();
        await Assert.That(persisted).HasSingleItem();
        await Assert.That(persisted[0].Title).IsEqualTo("song1");
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
    public async Task LoadAsync_WhenForceReloadStartsWhileInFlight_CancelsStaleLoadAndKeepsNewerResult()
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

        var (coordinator, recentContext, _, _, _) = CreateCoordinatorWithDb(db);

        Task staleLoad = coordinator.LoadAsync();
        Task freshLoad = coordinator.LoadAsync(forceReload: true);

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

        await Task.WhenAll(staleLoad, freshLoad);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(recentContext.Recent[0].Name).IsEqualTo("Fresh");
    }

    [Test]
    public async Task HandleQueueSessionStartedAsync_WhenLiveSingleMediaPlays_ReplacesRetainedMediaInstance()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, _, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var original = CreateDummyMedia("song1");
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(original), original);
        await Assert.That(((RecentMediaItemViewModel)recentContext.Recent[0]).Media).IsSameReferenceAs(original);

        // Simulate playing a different live MediaViewModel instance for the same media source
        var liveInstance = new MediaViewModel(original.Source) { Name = original.Name };
        await coordinator.HandleQueueSessionStartedAsync(QueueOrigin.ForSingleMedia(liveInstance), liveInstance);

        await Assert.That(recentContext.Recent).HasSingleItem();
        await Assert.That(((RecentMediaItemViewModel)recentContext.Recent[0]).Media).IsSameReferenceAs(liveInstance);
    }

    [Test]
    public async Task ReceiveMusicLibraryChanged_WhileShowRecentDisabled_InvalidatesLoadedStateSoReenablingRefreshesAlbum()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, libraryContext, _, dbService, generalSettings) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        string albumKey = AlbumViewModelFactory.GetAlbumKey("Abbey Road", "The Beatles");
        await dbService.UpsertRecentEntryAsync(new RecentEntryDto
        {
            Id = QueueOrigin.GetEntryId(RecentItemKind.Album, albumKey),
            Kind = RecentItemKind.Album,
            SourceId = albumKey,
            Title = "Abbey Road",
            Subtitle = "The Beatles",
            LastPlayed = DateTimeOffset.UtcNow
        });

        var albumV1 = new AlbumViewModel("Abbey Road", "The Beatles");
        var song1 = CreateDummyMedia("song1");
        albumV1.RelatedSongs.Add(song1);
        var libraryV1 = new MusicLibrary(
            new[] { song1 },
            new Dictionary<string, AlbumViewModel> { [albumKey] = albumV1 },
            new Dictionary<string, ArtistViewModel>(),
            Array.Empty<string>(),
            new AlbumViewModel(),
            new ArtistViewModel());
        libraryContext.Music = libraryV1;

        await coordinator.LoadAsync();
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(albumV1);

        // Disable ShowRecent while a library rescan produces a new AlbumViewModel instance
        generalSettings.ShowRecent = false;
        var albumV2 = new AlbumViewModel("Abbey Road", "The Beatles");
        var song2 = CreateDummyMedia("song2");
        albumV2.RelatedSongs.Add(song1);
        albumV2.RelatedSongs.Add(song2);
        var libraryV2 = new MusicLibrary(
            new[] { song1, song2 },
            new Dictionary<string, AlbumViewModel> { [albumKey] = albumV2 },
            new Dictionary<string, ArtistViewModel>(),
            Array.Empty<string>(),
            new AlbumViewModel(),
            new ArtistViewModel());
        libraryContext.Music = libraryV2;
        coordinator.Receive(new PropertyChangedMessage<MusicLibrary>(libraryContext, nameof(LibraryContext.Music), libraryV1, libraryV2));

        // Re-enabling ShowRecent and calling LoadAsync() (without forceReload) must refresh to albumV2
        generalSettings.ShowRecent = true;
        await coordinator.LoadAsync();
        await Assert.That(recentContext.Recent[0]).IsSameReferenceAs(albumV2);
    }

    [Test]
    public async Task LoadAsync_ForceReload_UpgradesSingleMediaToLinkedLibraryInstance()
    {
        using var fixture = new TestDirectoryFixture();
        var (coordinator, recentContext, libraryContext, _, dbService, _) = CreateCoordinator(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        const string songUrl = "https://example.com/song1.mp3";
        await dbService.UpsertRecentEntryAsync(new RecentEntryDto
        {
            Id = QueueOrigin.GetEntryId(RecentItemKind.SingleMedia, songUrl),
            Kind = RecentItemKind.SingleMedia,
            SourceId = songUrl,
            Title = "song1",
            LastPlayed = DateTimeOffset.UtcNow
        });

        // Initial load before library scan produces an unlinked MediaViewModel
        await coordinator.LoadAsync();
        var initialItem = (RecentMediaItemViewModel)recentContext.Recent[0];
        await Assert.That(initialItem.Media.Album).IsNull();

        // Populate music library with linked MediaViewModel carrying Album
        var album = new AlbumViewModel("Abbey Road", "The Beatles");
        var librarySong = CreateDummyMedia("song1");
        librarySong.Album = album;
        libraryContext.Music = new MusicLibrary(
            new[] { librarySong },
            new Dictionary<string, AlbumViewModel> { [album.AlbumKey] = album },
            new Dictionary<string, ArtistViewModel>(),
            Array.Empty<string>(),
            new AlbumViewModel(),
            new ArtistViewModel());

        // forceReload swaps in a wrapper around the linked library MediaViewModel
        await coordinator.LoadAsync(forceReload: true);
        var upgradedItem = (RecentMediaItemViewModel)recentContext.Recent[0];
        await Assert.That(upgradedItem.Media).IsSameReferenceAs(librarySong);
        await Assert.That(upgradedItem.Media.Album).IsSameReferenceAs(album);
    }
}

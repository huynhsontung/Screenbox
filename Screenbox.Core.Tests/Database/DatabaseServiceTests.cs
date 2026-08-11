using Microsoft.Data.Sqlite;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;

namespace Screenbox.Core.Tests.Database;

public sealed class DatabaseServiceTests
{
    [Test]
    public async Task InitializeAsync_CreatesDatabaseAndAllRequiredTables()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);

        await dbService.InitializeAsync();

        string dbPath = Path.Combine(fixture.DirectoryPath, "screenbox.db");
        await Assert.That(File.Exists(dbPath)).IsTrue().Because("Database file screenbox.db should be created.");

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        string[] requiredTables = ["library_folders", "media_records", "playback_progress", "playlists", "playlist_items"];
        foreach (string tableName in requiredTables)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name;";
            cmd.Parameters.AddWithValue("@name", tableName);
            long count = (long)(cmd.ExecuteScalar() ?? 0L);
            await Assert.That(count).IsEqualTo(1L);
        }
    }

    [Test]
    public async Task SaveMusicCacheAsync_And_LoadLibraryCacheAsync_PersistsAndRetrievesMusicRecords()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        List<string> folders = [@"C:\Music\Folder1", @"C:\Music\Folder2"];
        List<MusicCacheRecordDto> musicRecords =
        [
            new MusicCacheRecordDto
            {
                Path = @"C:\Music\Folder1\song1.mp3",
                Title = "Song One",
                Artist = "Artist Alpha",
                Album = "Album One",
                AlbumArtist = "Artist Alpha",
                Composers = "Composer X",
                Genre = "Rock",
                TrackNumber = 1,
                Bitrate = 320000,
                DateAdded = DateTimeOffset.UtcNow,
                Duration = TimeSpan.FromMinutes(3.5),
                Year = 2024,
            },
            new MusicCacheRecordDto
            {
                Path = @"C:\Music\Folder2\song2.flac",
                Title = "Song Two",
                Artist = "Artist Beta",
                Album = "Album Two",
                AlbumArtist = "Artist Beta",
                Composers = "Composer Y",
                Genre = "Jazz",
                TrackNumber = 2,
                Bitrate = 1411000,
                DateAdded = DateTimeOffset.UtcNow,
                Duration = TimeSpan.FromMinutes(4.2),
                Year = 2025,
            }
        ];

        await dbService.SaveMusicCacheAsync(folders, musicRecords);

        RawCacheLoadResultDto result = await dbService.LoadLibraryCacheAsync(MediaPlaybackType.Music);

        await Assert.That(result.FolderPaths.Count).IsEqualTo(2);
        await Assert.That(result.FolderPaths).Contains(@"C:\Music\Folder1");
        await Assert.That(result.FolderPaths).Contains(@"C:\Music\Folder2");

        await Assert.That(result.Records.Count).IsEqualTo(2);
        RawMediaRecordDto? song1 = result.Records.Find(r => r.Path == @"C:\Music\Folder1\song1.mp3");
        await Assert.That(song1).IsNotNull();
        await Assert.That(song1.Title).IsEqualTo("Song One");
        await Assert.That(song1.Artist).IsEqualTo("Artist Alpha");
        await Assert.That(song1.Album).IsEqualTo("Album One");
        await Assert.That(song1.TrackNumber).IsEqualTo(1u);
        await Assert.That(song1.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task SaveVideoCacheAsync_And_LoadLibraryCacheAsync_PersistsAndRetrievesVideoRecords()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        List<string> folders = [@"C:\Videos\Movies"];
        List<VideoCacheRecordDto> videoRecords =
        [
            new VideoCacheRecordDto
            {
                Path = @"C:\Videos\Movies\clip.mp4",
                Title = "Sample Clip",
                Subtitle = "Eng",
                Producers = "Producer A",
                Writers = "Writer B",
                Width = 1920,
                Height = 1080,
                VideoBitrate = 5000000,
                DateAdded = DateTimeOffset.UtcNow,
                Duration = TimeSpan.FromHours(1.5),
                Year = 2023,
            }
        ];

        await dbService.SaveVideoCacheAsync(folders, videoRecords);

        RawCacheLoadResultDto result = await dbService.LoadLibraryCacheAsync(MediaPlaybackType.Video);

        await Assert.That(result.FolderPaths).HasSingleItem();
        await Assert.That(result.FolderPaths[0]).IsEqualTo(@"C:\Videos\Movies");

        await Assert.That(result.Records).HasSingleItem();
        RawMediaRecordDto video = result.Records[0];
        await Assert.That(video.Path).IsEqualTo(@"C:\Videos\Movies\clip.mp4");
        await Assert.That(video.Title).IsEqualTo("Sample Clip");
        await Assert.That(video.Width).IsEqualTo(1920u);
        await Assert.That(video.Height).IsEqualTo(1080u);
        await Assert.That(video.MediaType).IsEqualTo(MediaPlaybackType.Video);
    }

    [Test]
    public async Task SaveMusicCacheAsync_ClearsStaleRecordsOnRescan()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        // Initial save with two songs
        List<string> folders = [@"C:\Music\Folder1", @"C:\Music\Folder2"];
        List<MusicCacheRecordDto> initialRecords =
        [
            new MusicCacheRecordDto { Path = @"C:\Music\Folder1\song1.mp3", Title = "Song One" },
            new MusicCacheRecordDto { Path = @"C:\Music\Folder2\song2.mp3", Title = "Song Two" },
        ];
        await dbService.SaveMusicCacheAsync(folders, initialRecords);

        // Re-save with only one folder/song (simulating folder removal + refresh)
        List<string> updatedFolders = [@"C:\Music\Folder1"];
        List<MusicCacheRecordDto> updatedRecords =
        [
            new MusicCacheRecordDto { Path = @"C:\Music\Folder1\song1.mp3", Title = "Song One" },
        ];
        await dbService.SaveMusicCacheAsync(updatedFolders, updatedRecords);

        RawCacheLoadResultDto result = await dbService.LoadLibraryCacheAsync(MediaPlaybackType.Music);

        await Assert.That(result.Records).HasSingleItem();
        await Assert.That(result.Records[0].Path).IsEqualTo(@"C:\Music\Folder1\song1.mp3");
    }

    [Test]
    public async Task SaveVideoCacheAsync_ClearsStaleRecordsOnRescan()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        // Initial save with two videos
        List<string> folders = [@"C:\Videos\Folder1", @"C:\Videos\Folder2"];
        List<VideoCacheRecordDto> initialRecords =
        [
            new VideoCacheRecordDto { Path = @"C:\Videos\Folder1\video1.mp4", Title = "Video One" },
            new VideoCacheRecordDto { Path = @"C:\Videos\Folder2\video2.mp4", Title = "Video Two" },
        ];
        await dbService.SaveVideoCacheAsync(folders, initialRecords);

        // Re-save with only one folder/video (simulating folder removal + refresh)
        List<string> updatedFolders = [@"C:\Videos\Folder1"];
        List<VideoCacheRecordDto> updatedRecords =
        [
            new VideoCacheRecordDto { Path = @"C:\Videos\Folder1\video1.mp4", Title = "Video One" },
        ];
        await dbService.SaveVideoCacheAsync(updatedFolders, updatedRecords);

        RawCacheLoadResultDto result = await dbService.LoadLibraryCacheAsync(MediaPlaybackType.Video);

        await Assert.That(result.Records).HasSingleItem();
        await Assert.That(result.Records[0].Path).IsEqualTo(@"C:\Videos\Folder1\video1.mp4");
    }

    [Test]
    public async Task PlaylistOperations_SaveLoadListAndDelete_BehavesCorrectly()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        var playlistDto = new PlaylistRecordDto
        {
            Id = "pl_001",
            DisplayName = "My Favorites",
            LastUpdated = DateTimeOffset.UtcNow,
            Items =
            [
                new RawMediaRecordDto { Path = @"C:\Media\track1.mp3", Title = "Track 1" },
                new RawMediaRecordDto { Path = @"C:\Media\track2.mp3", Title = "Track 2" },
            ]
        };

        // 1. Save Playlist
        await dbService.SavePlaylistAsync(playlistDto);

        // 2. Load Playlist
        PlaylistRecordDto? loaded = await dbService.LoadPlaylistAsync("pl_001");
        await Assert.That(loaded).IsNotNull();
        await Assert.That(loaded.Id).IsEqualTo("pl_001");
        await Assert.That(loaded.DisplayName).IsEqualTo("My Favorites");
        await Assert.That(loaded.Items.Count).IsEqualTo(2);
        await Assert.That(loaded.Items[0].Path).IsEqualTo(@"C:\Media\track1.mp3");
        await Assert.That(loaded.Items[1].Path).IsEqualTo(@"C:\Media\track2.mp3");

        // 3. List Playlists
        List<PlaylistRecordDto> playlists = await dbService.ListPlaylistsAsync();
        await Assert.That(playlists).HasSingleItem();
        await Assert.That(playlists[0].Id).IsEqualTo("pl_001");

        // 4. Delete Playlist
        await dbService.DeletePlaylistAsync("pl_001");
        PlaylistRecordDto? deleted = await dbService.LoadPlaylistAsync("pl_001");
        await Assert.That(deleted).IsNull();
    }

    [Test]
    public async Task PlaybackProgressOperations_SaveAndLoad_RoundtripsPositionTicks()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        string location = @"C:\Media\movie.mkv";
        TimeSpan expectedPosition = TimeSpan.FromMinutes(42.5);

        List<MediaPlaybackProgress> snapshot = [new MediaPlaybackProgress(location, expectedPosition)];
        await dbService.ReplacePlaybackProgressAsync(snapshot);

        List<MediaPlaybackProgress> loadedList = await dbService.LoadPlaybackProgressAsync();
        await Assert.That(loadedList).HasSingleItem();
        await Assert.That(loadedList[0].Location).IsEqualTo(location);
        await Assert.That(loadedList[0].Position.Ticks).IsEqualTo(expectedPosition.Ticks);
    }
}

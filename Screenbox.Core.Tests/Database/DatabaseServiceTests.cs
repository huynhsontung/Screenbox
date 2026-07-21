#nullable enable

using Microsoft.Data.Sqlite;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Xunit;

namespace Screenbox.Core.Tests.Database;

public sealed class DatabaseServiceTests
{
    [Fact]
    public async Task InitializeAsync_CreatesDatabaseAndAllRequiredTables()
    {
        using var fixture = new TestDirectoryFixture();
        var dbService = new DatabaseService(fixture.DirectoryPath);

        await dbService.InitializeAsync();

        string dbPath = Path.Combine(fixture.DirectoryPath, "screenbox.db");
        Assert.True(File.Exists(dbPath), "Database file screenbox.db should be created.");

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        string[] requiredTables = ["library_folders", "media_records", "playback_progress", "playlists", "playlist_items"];
        foreach (string tableName in requiredTables)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name;";
            cmd.Parameters.AddWithValue("@name", tableName);
            long count = (long)(cmd.ExecuteScalar() ?? 0L);
            Assert.Equal(1L, count);
        }
    }

    [Fact]
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

        Assert.Equal(2, result.FolderPaths.Count);
        Assert.Contains(@"C:\Music\Folder1", result.FolderPaths);
        Assert.Contains(@"C:\Music\Folder2", result.FolderPaths);

        Assert.Equal(2, result.Records.Count);
        RawMediaRecordDto? song1 = result.Records.Find(r => r.Path == @"C:\Music\Folder1\song1.mp3");
        Assert.NotNull(song1);
        Assert.Equal("Song One", song1.Title);
        Assert.Equal("Artist Alpha", song1.Artist);
        Assert.Equal("Album One", song1.Album);
        Assert.Equal(1u, song1.TrackNumber);
        Assert.Equal(MediaPlaybackType.Music, song1.MediaType);
    }

    [Fact]
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

        Assert.Single(result.FolderPaths);
        Assert.Equal(@"C:\Videos\Movies", result.FolderPaths[0]);

        Assert.Single(result.Records);
        RawMediaRecordDto video = result.Records[0];
        Assert.Equal(@"C:\Videos\Movies\clip.mp4", video.Path);
        Assert.Equal("Sample Clip", video.Title);
        Assert.Equal(1920u, video.Width);
        Assert.Equal(1080u, video.Height);
        Assert.Equal(MediaPlaybackType.Video, video.MediaType);
    }

    [Fact]
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
        Assert.NotNull(loaded);
        Assert.Equal("pl_001", loaded.Id);
        Assert.Equal("My Favorites", loaded.DisplayName);
        Assert.Equal(2, loaded.Items.Count);
        Assert.Equal(@"C:\Media\track1.mp3", loaded.Items[0].Path);
        Assert.Equal(@"C:\Media\track2.mp3", loaded.Items[1].Path);

        // 3. List Playlists
        List<PlaylistRecordDto> playlists = await dbService.ListPlaylistsAsync();
        Assert.Single(playlists);
        Assert.Equal("pl_001", playlists[0].Id);

        // 4. Delete Playlist
        await dbService.DeletePlaylistAsync("pl_001");
        PlaylistRecordDto? deleted = await dbService.LoadPlaylistAsync("pl_001");
        Assert.Null(deleted);
    }

    [Fact]
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
        Assert.Single(loadedList);
        Assert.Equal(location, loadedList[0].Location);
        Assert.Equal(expectedPosition.Ticks, loadedList[0].Position.Ticks);
    }
}

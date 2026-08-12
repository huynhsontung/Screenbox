#nullable enable

using System.Text.Json;
using Screenbox.Core.Models;
using Screenbox.Core.Models.Serialization;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;

namespace Screenbox.Core.Tests.Database;

public sealed class LegacyMigrationTests
{
    [Test]
    public async Task InitializeAsync_MigratesLegacyJsonPlaylistsToSql_AndCleansUpLegacyFiles()
    {
        using var fixture = new TestDirectoryFixture();

        // 1. Setup legacy Playlists directory and legacy JSON file
        string playlistsDirPath = Path.Combine(fixture.DirectoryPath, "Playlists");
        Directory.CreateDirectory(playlistsDirPath);

        var legacyPlaylist = new PlaylistRecordDto
        {
            Id = "legacy_pl_123",
            DisplayName = "Classic Rock",
            LastUpdated = DateTimeOffset.UtcNow.AddDays(-10),
            Items =
            [
                new RawMediaRecordDto { Path = @"C:\Music\Rock\stairway.flac", Title = "Stairway to Heaven" },
                new RawMediaRecordDto { Path = @"C:\Music\Rock\hotel_california.flac", Title = "Hotel California" }
            ]
        };

        string json = JsonSerializer.Serialize(legacyPlaylist, CoreJsonContext.Default.PlaylistRecordDto);

        string legacyFilePath = Path.Combine(playlistsDirPath, "ClassicRock.json");
        await File.WriteAllTextAsync(legacyFilePath, json);

        // Also create legacy bin files to test cleanup
        string songsBinPath = Path.Combine(fixture.DirectoryPath, "songs.bin");
        await File.WriteAllTextAsync(songsBinPath, "dummy_legacy_binary_data");

        // 2. Initialize DatabaseService (triggers legacy import and cleanup)
        var dbService = new DatabaseService(fixture.DirectoryPath);
        await dbService.InitializeAsync();

        // 3. Verify playlist was imported into SQL database
        PlaylistRecordDto? importedPlaylist = await dbService.LoadPlaylistAsync("legacy_pl_123");
        await Assert.That(importedPlaylist).IsNotNull();
        await Assert.That(importedPlaylist.Id).IsEqualTo("legacy_pl_123");
        await Assert.That(importedPlaylist.DisplayName).IsEqualTo("Classic Rock");
        await Assert.That(importedPlaylist.Items.Count).IsEqualTo(2);
        await Assert.That(importedPlaylist.Items[0].Path).IsEqualTo(@"C:\Music\Rock\stairway.flac");
        await Assert.That(importedPlaylist.Items[1].Path).IsEqualTo(@"C:\Music\Rock\hotel_california.flac");

        // 4. Verify post-migration cleanup deleted the legacy Playlists directory and legacy files
        await Assert.That(Directory.Exists(playlistsDirPath)).IsFalse().Because("Legacy Playlists directory should be deleted after successful migration.");
        await Assert.That(File.Exists(songsBinPath)).IsFalse().Because("Legacy songs.bin file should be deleted after successful migration.");
    }

    [Test]
    public async Task InitializeAsync_WhenSqlDatabaseAlreadyHasPlaylists_DoesNotReimportLegacyFiles()
    {
        using var fixture = new TestDirectoryFixture();

        // 1. Pre-initialize DB and insert an existing playlist
        var dbService1 = new DatabaseService(fixture.DirectoryPath);
        await dbService1.InitializeAsync();

        await dbService1.SavePlaylistAsync(new PlaylistRecordDto
        {
            Id = "existing_sql_pl",
            DisplayName = "Existing SQL Playlist",
            LastUpdated = DateTimeOffset.UtcNow,
            Items = []
        });

        // 2. Add a legacy playlist file afterwards
        string playlistsDirPath = Path.Combine(fixture.DirectoryPath, "Playlists");
        Directory.CreateDirectory(playlistsDirPath);

        var legacyPlaylist = new PlaylistRecordDto
        {
            Id = "ignored_legacy_pl",
            DisplayName = "Should Be Ignored",
            LastUpdated = DateTimeOffset.UtcNow,
            Items = []
        };

        string json = JsonSerializer.Serialize(legacyPlaylist, CoreJsonContext.Default.PlaylistRecordDto);
        await File.WriteAllTextAsync(Path.Combine(playlistsDirPath, "Ignored.json"), json);

        // 3. Re-initialize DatabaseService
        var dbService2 = new DatabaseService(fixture.DirectoryPath);
        await dbService2.InitializeAsync();

        // 4. Verify the existing playlist remains and the legacy one was NOT imported
        PlaylistRecordDto? existing = await dbService2.LoadPlaylistAsync("existing_sql_pl");
        await Assert.That(existing).IsNotNull();

        PlaylistRecordDto? legacy = await dbService2.LoadPlaylistAsync("ignored_legacy_pl");
        await Assert.That(legacy).IsNull();
    }
}

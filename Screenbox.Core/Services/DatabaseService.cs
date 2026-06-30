#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Windows.Storage;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="IDatabaseService"/> using a single SQLite file stored in
/// <see cref="ApplicationData.LocalFolder"/>.
/// Schema drift is recovered per table where possible so playlist data can be preserved.
/// The database file is deleted only as a last-resort recovery path.
/// </summary>
public sealed class DatabaseService : IDatabaseService
{
    private const string DbFileName = "screenbox.db";
    private const string LegacyPlaylistsFolderName = "Playlists";
    private static readonly string[] LegacyLocalFileNames = ["songs.bin", "videos.bin"];
    private const string LegacyTemporaryFileName = "last_positions.bin";

    private string? _connectionString;
    private readonly object _initLock = new();
    private Task? _initializationTask;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await EnsureInitializedAsync();
    }

    /// <inheritdoc/>
    public async Task<RawCacheLoadResultDto> LoadLibraryCacheAsync(MediaPlaybackType mediaType)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();

        var folderPaths = new List<string>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT path FROM library_folders WHERE media_type = @mt;";
            cmd.Parameters.AddWithValue("@mt", (int)mediaType);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                folderPaths.Add(reader.GetString(0));
            }
        }

        var records = new List<RawMediaRecordDto>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = SelectMediaRecordsByTypeSql;
            cmd.Parameters.AddWithValue("@mt", (int)mediaType);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(ReadRawRecord(reader));
            }
        }

        return new RawCacheLoadResultDto
        {
            FolderPaths = folderPaths,
            Records = records,
        };
    }

    /// <inheritdoc/>
    public async Task SaveMusicCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<MusicCacheRecordDto> records)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, "DELETE FROM library_folders WHERE media_type = @mt;",
            new SqlParameterDto { Name = "@mt", Value = (int)MediaPlaybackType.Music });

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO library_folders (path, media_type) VALUES (@path, @mt);";
            var pathParam = cmd.Parameters.Add("@path", SqliteType.Text);
            cmd.Parameters.AddWithValue("@mt", (int)MediaPlaybackType.Music);
            foreach (string path in folderPaths)
            {
                pathParam.Value = path;
                cmd.ExecuteNonQuery();
            }
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = UpsertMusicMediaRecordSql;

            var p = cmd.Parameters;
            var pPath = p.Add("@path", SqliteType.Text);
            var pTitle = p.Add("@title", SqliteType.Text);
            p.AddWithValue("@mt", (int)MediaPlaybackType.Music);
            var pDateAdded = p.Add("@dateAdded", SqliteType.Integer);
            var pDuration = p.Add("@durationTicks", SqliteType.Integer);
            var pYear = p.Add("@year", SqliteType.Integer);
            var pArtist = p.Add("@artist", SqliteType.Text);
            var pAlbum = p.Add("@album", SqliteType.Text);
            var pAlbumArtist = p.Add("@albumArtist", SqliteType.Text);
            var pComposers = p.Add("@composers", SqliteType.Text);
            var pGenre = p.Add("@genre", SqliteType.Text);
            var pTrack = p.Add("@trackNumber", SqliteType.Integer);
            var pBitrate = p.Add("@bitrate", SqliteType.Integer);

            foreach (MusicCacheRecordDto record in records)
            {
                pPath.Value = record.Path;
                pTitle.Value = record.Title;
                pDateAdded.Value = record.DateAdded.UtcDateTime.Ticks;
                pDuration.Value = record.Info.Duration.Ticks;
                pYear.Value = (long)record.Info.Year;
                pArtist.Value = record.Info.Artist;
                pAlbum.Value = record.Info.Album;
                pAlbumArtist.Value = record.Info.AlbumArtist;
                pComposers.Value = record.Info.Composers;
                pGenre.Value = record.Info.Genre;
                pTrack.Value = (long)record.Info.TrackNumber;
                pBitrate.Value = (long)record.Info.Bitrate;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    /// <inheritdoc/>
    public async Task SaveVideoCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<VideoCacheRecordDto> records)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, "DELETE FROM library_folders WHERE media_type = @mt;",
            new SqlParameterDto { Name = "@mt", Value = (int)MediaPlaybackType.Video });

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO library_folders (path, media_type) VALUES (@path, @mt);";
            var pathParam = cmd.Parameters.Add("@path", SqliteType.Text);
            cmd.Parameters.AddWithValue("@mt", (int)MediaPlaybackType.Video);
            foreach (string path in folderPaths)
            {
                pathParam.Value = path;
                cmd.ExecuteNonQuery();
            }
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = UpsertVideoMediaRecordSql;

            var p = cmd.Parameters;
            var pPath = p.Add("@path", SqliteType.Text);
            var pTitle = p.Add("@title", SqliteType.Text);
            p.AddWithValue("@mt", (int)MediaPlaybackType.Video);
            var pDateAdded = p.Add("@dateAdded", SqliteType.Integer);
            var pDuration = p.Add("@durationTicks", SqliteType.Integer);
            var pYear = p.Add("@year", SqliteType.Integer);
            var pSubtitle = p.Add("@subtitle", SqliteType.Text);
            var pProducers = p.Add("@producers", SqliteType.Text);
            var pWriters = p.Add("@writers", SqliteType.Text);
            var pWidth = p.Add("@width", SqliteType.Integer);
            var pHeight = p.Add("@height", SqliteType.Integer);
            var pVideoBitrate = p.Add("@videoBitrate", SqliteType.Integer);

            foreach (VideoCacheRecordDto record in records)
            {
                pPath.Value = record.Path;
                pTitle.Value = record.Title;
                pDateAdded.Value = record.DateAdded.UtcDateTime.Ticks;
                pDuration.Value = record.Info.Duration.Ticks;
                pYear.Value = (long)record.Info.Year;
                pSubtitle.Value = record.Info.Subtitle;
                pProducers.Value = record.Info.Producers;
                pWriters.Value = record.Info.Writers;
                pWidth.Value = (long)record.Info.Width;
                pHeight.Value = (long)record.Info.Height;
                pVideoBitrate.Value = (long)record.Info.Bitrate;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    /// <inheritdoc/>
    public async Task ReplacePlaybackProgressAsync(IReadOnlyList<MediaPlaybackProgress> snapshot)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, "DELETE FROM playback_progress;");

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO playback_progress (location, position_ticks) VALUES (@loc, @ticks);";
            var pLoc = cmd.Parameters.Add("@loc", SqliteType.Text);
            var pTicks = cmd.Parameters.Add("@ticks", SqliteType.Integer);

            foreach (MediaPlaybackProgress item in snapshot)
            {
                pLoc.Value = item.Location;
                pTicks.Value = item.Position.Ticks;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    /// <inheritdoc/>
    public async Task<List<MediaPlaybackProgress>> LoadPlaybackProgressAsync()
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT location, position_ticks FROM playback_progress;";

        var result = new List<MediaPlaybackProgress>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string location = reader.GetString(0);
            long ticks = reader.GetInt64(1);
            result.Add(new MediaPlaybackProgress(location, new TimeSpan(ticks)));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task SavePlaylistAsync(PersistentPlaylist playlist)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                INSERT OR REPLACE INTO playlists (id, display_name, last_updated)
                VALUES (@id, @name, @updated);
                """;
            cmd.Parameters.AddWithValue("@id", playlist.Id);
            cmd.Parameters.AddWithValue("@name", playlist.DisplayName);
            cmd.Parameters.AddWithValue("@updated", playlist.LastUpdated.UtcTicks);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM playlist_items WHERE playlist_id = @id;";
            cmd.Parameters.AddWithValue("@id", playlist.Id);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO playlist_items
                    (playlist_id, path, sort_order)
                VALUES
                    (@pid, @path, @order);
                """;

            var p = cmd.Parameters;
            p.AddWithValue("@pid", playlist.Id);
            var pPath = p.Add("@path", SqliteType.Text);
            var pOrder = p.Add("@order", SqliteType.Integer);

            for (int i = 0; i < playlist.Items.Count; i++)
            {
                PersistentMediaRecord item = playlist.Items[i];
                pPath.Value = item.Path;
                pOrder.Value = i;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    /// <inheritdoc/>
    public async Task<PersistentPlaylist?> LoadPlaylistAsync(string id)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        PersistentPlaylist? playlist = null;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id, display_name, last_updated FROM playlists WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                playlist = new PersistentPlaylist
                {
                    Id = reader.GetString(0),
                    DisplayName = reader.GetString(1),
                    LastUpdated = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero),
                };
            }
        }

        if (playlist is null)
        {
            return null;
        }

        playlist.Items = ReadPlaylistItems(connection, id);
        return playlist;
    }

    /// <inheritdoc/>
    public async Task<List<PersistentPlaylist>> ListPlaylistsAsync()
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();

        var playlists = new List<PersistentPlaylist>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id, display_name, last_updated FROM playlists ORDER BY last_updated DESC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                playlists.Add(new PersistentPlaylist
                {
                    Id = reader.GetString(0),
                    DisplayName = reader.GetString(1),
                    LastUpdated = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero),
                });
            }
        }

        foreach (PersistentPlaylist playlist in playlists)
        {
            playlist.Items = ReadPlaylistItems(connection, playlist.Id);
        }

        return playlists;
    }

    /// <inheritdoc/>
    public async Task DeletePlaylistAsync(string id)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM playlists WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection CreateConnection()
    {
        if (_connectionString is null)
        {
            throw new InvalidOperationException("DatabaseService is not initialized. Call InitializeAsync before creating connections.");
        }

        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Foreign-key enforcement must be re-enabled per connection.
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();

        return connection;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task EnsureInitializedAsync()
    {
        Task? initializationTask;

        lock (_initLock)
        {
            _initializationTask ??= InitializeCoreAsync();
            initializationTask = _initializationTask;
        }

        await initializationTask;
    }

    private async Task InitializeCoreAsync()
    {
        string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();

        // Assign early so CreateSchema() can use it via CreateConnection().
        _connectionString = connectionString;

        try
        {
            await EnsureSchemaAsync();
        }
        catch (Exception ex) when (ex is SqliteException or IOException)
        {
            // Last resort: if recovery cannot succeed, recreate the database file.
            LogService.Log(ex);
            await RecreateDatabaseFileAsync(dbPath, connectionString);
        }
    }

    private async Task EnsureSchemaAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        ExecuteNonQuery(connection, "PRAGMA journal_mode=WAL;");
        ExecuteNonQuery(connection, "PRAGMA foreign_keys=OFF;");

        bool migrationComplete;
        using var transaction = connection.BeginTransaction();
        EnsureReplaceableTable(connection, "library_folders", CreateLibraryFoldersSql, "id", "path", "media_type");
        EnsureReplaceableTable(connection, "media_records", CreateMediaRecordsSql,
            "path", "title", "media_type", "date_added", "duration_ticks", "year",
            "artist", "album", "album_artist", "composers", "genre", "track_number", "bitrate",
            "subtitle", "producers", "writers", "width", "height", "video_bitrate");
        EnsureReplaceableTable(connection, "playback_progress", CreatePlaybackProgressSql, "location", "position_ticks");
        EnsurePlaylistsTable(connection);
        EnsurePlaylistItemsTable(connection);
        migrationComplete = await TryImportLegacyPlaylistsAsync(connection);
        transaction.Commit();
        ExecuteNonQuery(connection, "PRAGMA foreign_keys=ON;");

        if (migrationComplete)
        {
            await TryDeleteLegacyMigrationArtifactsAsync();
        }
    }

    private async Task RecreateDatabaseFileAsync(string dbPath, string connectionString)
    {
        _connectionString = null;
        await TryDeleteDatabaseAsync(dbPath);
        _connectionString = connectionString;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        ExecuteNonQuery(connection, "PRAGMA journal_mode=WAL;");
        ExecuteNonQuery(connection, "PRAGMA foreign_keys=OFF;");

        using var transaction = connection.BeginTransaction();
        ExecuteNonQuery(connection, CreateLibraryFoldersSql);
        ExecuteNonQuery(connection, CreateMediaRecordsSql);
        ExecuteNonQuery(connection, CreatePlaybackProgressSql);
        ExecuteNonQuery(connection, CreatePlaylistsSql);
        ExecuteNonQuery(connection, CreatePlaylistItemsSql);
        transaction.Commit();
        ExecuteNonQuery(connection, "PRAGMA foreign_keys=ON;");
    }

    private static void EnsureReplaceableTable(SqliteConnection connection, string tableName, string createSql, params string[] expectedColumns)
    {
        HashSet<string> actualColumns = ReadTableColumns(connection, tableName);
        if (actualColumns.Count is 0)
        {
            ExecuteNonQuery(connection, createSql);
            return;
        }

        if (HasSchemaDrift(actualColumns, expectedColumns))
        {
            ExecuteNonQuery(connection, $"DROP TABLE IF EXISTS {tableName};");
            ExecuteNonQuery(connection, createSql);
        }
    }

    private static void EnsurePlaylistsTable(SqliteConnection connection)
    {
        HashSet<string> actualColumns = ReadTableColumns(connection, "playlists");
        string[] expectedColumns = ["id", "display_name", "last_updated"];
        if (actualColumns.Count is 0)
        {
            ExecuteNonQuery(connection, CreatePlaylistsSql);
            return;
        }

        if (!HasSchemaDrift(actualColumns, expectedColumns))
        {
            return;
        }

        ExecuteNonQuery(connection, "DROP TABLE playlists;");
        ExecuteNonQuery(connection, CreatePlaylistsSql);
    }

    private static void EnsurePlaylistItemsTable(SqliteConnection connection)
    {
        HashSet<string> actualColumns = ReadTableColumns(connection, "playlist_items");
        string[] expectedColumns = ["id", "playlist_id", "path", "sort_order"];
        if (actualColumns.Count is 0)
        {
            ExecuteNonQuery(connection, CreatePlaylistItemsSql);
            return;
        }

        if (!HasSchemaDrift(actualColumns, expectedColumns))
        {
            return;
        }

        ExecuteNonQuery(connection, "DROP TABLE playlist_items;");
        ExecuteNonQuery(connection, CreatePlaylistItemsSql);
    }

    private static async Task<bool> TryImportLegacyPlaylistsAsync(SqliteConnection connection)
    {
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        IStorageItem? playlistsItem;
        try
        {
            playlistsItem = await localFolder.TryGetItemAsync(LegacyPlaylistsFolderName);
        }
        catch (Exception ex)
        {
            LogService.Log($"Failed to locate legacy playlists folder '{LegacyPlaylistsFolderName}': {ex.Message}");
            return false;
        }

        if (TableHasRows(connection, "playlists"))
        {
            return true;
        }

        if (playlistsItem is not StorageFolder playlistsFolder)
        {
            return true;
        }

        IReadOnlyList<StorageFile> playlistFiles;
        try
        {
            playlistFiles = await playlistsFolder.GetFilesAsync();
        }
        catch (Exception ex)
        {
            LogService.Log($"Failed to list legacy playlists in '{playlistsFolder.Path}': {ex.Message}");
            return false;
        }

        List<StorageFile> jsonPlaylistFiles = playlistFiles
            .Where(file => string.Equals(file.FileType, ".json", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (jsonPlaylistFiles.Count is 0)
        {
            return true;
        }

        using var upsertPlaylistCommand = connection.CreateCommand();
        upsertPlaylistCommand.CommandText = """
            INSERT OR REPLACE INTO playlists (id, display_name, last_updated)
            VALUES (@id, @name, @updated);
            """;
        var playlistIdParameter = upsertPlaylistCommand.Parameters.Add("@id", SqliteType.Text);
        var playlistNameParameter = upsertPlaylistCommand.Parameters.Add("@name", SqliteType.Text);
        var playlistUpdatedParameter = upsertPlaylistCommand.Parameters.Add("@updated", SqliteType.Integer);

        using var clearItemsCommand = connection.CreateCommand();
        clearItemsCommand.CommandText = "DELETE FROM playlist_items WHERE playlist_id = @id;";
        var clearItemsPlaylistIdParameter = clearItemsCommand.Parameters.Add("@id", SqliteType.Text);

        using var insertItemCommand = connection.CreateCommand();
        insertItemCommand.CommandText = """
            INSERT INTO playlist_items (playlist_id, path, sort_order)
            VALUES (@pid, @path, @order);
            """;
        var itemPlaylistIdParameter = insertItemCommand.Parameters.Add("@pid", SqliteType.Text);
        var itemPathParameter = insertItemCommand.Parameters.Add("@path", SqliteType.Text);
        var itemOrderParameter = insertItemCommand.Parameters.Add("@order", SqliteType.Integer);

        bool hasImportFailure = false;
        foreach (StorageFile playlistFile in jsonPlaylistFiles)
        {
            PersistentPlaylist? playlist = await TryReadLegacyPlaylistAsync(playlistFile);
            if (playlist is null || string.IsNullOrWhiteSpace(playlist.Id))
            {
                hasImportFailure = true;
                continue;
            }

            playlistIdParameter.Value = playlist.Id;
            playlistNameParameter.Value = string.IsNullOrWhiteSpace(playlist.DisplayName) ? playlist.Id : playlist.DisplayName;
            playlistUpdatedParameter.Value = playlist.LastUpdated == default
                ? DateTimeOffset.UtcNow.UtcTicks
                : playlist.LastUpdated.UtcTicks;
            upsertPlaylistCommand.ExecuteNonQuery();

            clearItemsPlaylistIdParameter.Value = playlist.Id;
            clearItemsCommand.ExecuteNonQuery();

            for (int i = 0; i < playlist.Items.Count; i++)
            {
                PersistentMediaRecord item = playlist.Items[i];
                if (string.IsNullOrWhiteSpace(item.Path))
                {
                    continue;
                }

                itemPlaylistIdParameter.Value = playlist.Id;
                itemPathParameter.Value = item.Path;
                itemOrderParameter.Value = i;
                insertItemCommand.ExecuteNonQuery();
            }
        }

        return !hasImportFailure;
    }

    private static async Task<PersistentPlaylist?> TryReadLegacyPlaylistAsync(StorageFile playlistFile)
    {
        try
        {
            string json = await FileIO.ReadTextAsync(playlistFile);
            return JsonSerializer.Deserialize<PersistentPlaylist>(json);
        }
        catch (Exception ex) when (ex is JsonException)
        {
            LogService.Log($"Failed to import legacy playlist '{playlistFile.Path}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            LogService.Log($"Failed to read legacy playlist '{playlistFile.Path}': {ex.Message}");
            return null;
        }
    }

    private static bool TableHasRows(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM {tableName} LIMIT 1);";
        return cmd.ExecuteScalar() is long value && value == 1;
    }

    private static async Task TryDeleteLegacyMigrationArtifactsAsync()
    {
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;

        foreach (string fileName in LegacyLocalFileNames)
        {
            await TryDeleteStorageItemAsync(localFolder, fileName);
        }

        await TryDeleteStorageItemAsync(temporaryFolder, LegacyTemporaryFileName);
        await TryDeleteStorageItemAsync(localFolder, LegacyPlaylistsFolderName);
    }

    private static async Task TryDeleteStorageItemAsync(StorageFolder folder, string itemName)
    {
        try
        {
            IStorageItem? item = await folder.TryGetItemAsync(itemName);
            if (item is null)
            {
                return;
            }

            if (item is StorageFolder storageFolder)
            {
                await storageFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                return;
            }

            await item.DeleteAsync();
        }
        catch (Exception ex)
        {
            LogService.Log($"Failed to delete legacy item '{Path.Combine(folder.Path, itemName)}': {ex.Message}");
        }
    }

    private static HashSet<string> ReadTableColumns(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = cmd.ExecuteReader();

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    private static bool HasSchemaDrift(HashSet<string> actualColumns, params string[] expectedColumns)
    {
        return !actualColumns.SetEquals(expectedColumns);
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql, params SqlParameterDto[] parameters)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (SqlParameterDto parameter in parameters)
        {
            cmd.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        cmd.ExecuteNonQuery();
    }

    private static RawMediaRecordDto ReadRawRecord(SqliteDataReader reader)
    {
        return new RawMediaRecordDto
        {
            Path = reader.GetString(0),
            Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            MediaType = (MediaPlaybackType)(reader.IsDBNull(2) ? 0 : reader.GetInt32(2)),
            DateAddedTicks = reader.IsDBNull(3) ? 0L : reader.GetInt64(3),
            DurationTicks = reader.IsDBNull(4) ? 0L : reader.GetInt64(4),
            Year = reader.IsDBNull(5) ? 0u : (uint)reader.GetInt64(5),
            Artist = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Album = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            AlbumArtist = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            Composers = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            Genre = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            TrackNumber = reader.IsDBNull(11) ? 0u : (uint)reader.GetInt64(11),
            Bitrate = reader.IsDBNull(12) ? 0u : (uint)reader.GetInt64(12),
            Subtitle = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            Producers = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            Writers = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            Width = reader.IsDBNull(16) ? 0u : (uint)reader.GetInt64(16),
            Height = reader.IsDBNull(17) ? 0u : (uint)reader.GetInt64(17),
            VideoBitrate = reader.IsDBNull(18) ? 0u : (uint)reader.GetInt64(18),
        };
    }

    private static List<PersistentMediaRecord> ReadPlaylistItems(SqliteConnection connection, string playlistId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = SelectPlaylistItemsWithMetadataSql;
        cmd.Parameters.AddWithValue("@pid", playlistId);

        var items = new List<PersistentMediaRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string path = reader.GetString(0);
            string fallbackTitle = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(fallbackTitle))
            {
                fallbackTitle = path;
            }

            string title = reader.IsDBNull(1) ? fallbackTitle : reader.GetString(1);
            var mediaType = (MediaPlaybackType)(reader.IsDBNull(2) ? (int)MediaPlaybackType.Unknown : reader.GetInt32(2));
            DateTime dateAdded = reader.IsDBNull(3) ? default : new DateTime(reader.GetInt64(3), DateTimeKind.Utc);
            long durationTicks = reader.IsDBNull(4) ? 0L : reader.GetInt64(4);
            uint year = reader.IsDBNull(5) ? 0u : (uint)reader.GetInt64(5);

            IMediaProperties properties;
            if (mediaType == MediaPlaybackType.Music)
            {
                properties = new MusicInfo
                {
                    Title = title,
                    Artist = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Album = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    AlbumArtist = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    Composers = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    Genre = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    TrackNumber = reader.IsDBNull(11) ? 0u : (uint)reader.GetInt64(11),
                    Year = year,
                    Duration = new TimeSpan(durationTicks),
                };
            }
            else
            {
                properties = new VideoInfo
                {
                    Title = title,
                    Subtitle = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    Producers = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    Writers = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                    Year = year,
                    Duration = new TimeSpan(durationTicks),
                };
            }

            items.Add(new PersistentMediaRecord
            {
                Title = title,
                Path = path,
                MediaType = mediaType,
                DateAdded = dateAdded,
                Duration = new TimeSpan(durationTicks),
                Year = year,
                Properties = properties,
            });
        }

        return items;
    }

    private static async Task TryDeleteDatabaseAsync(string dbPath)
    {
        // Also remove WAL and shared-memory sidecar files.
        string? directoryPath = Path.GetDirectoryName(dbPath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        StorageFolder folder;
        try
        {
            folder = await StorageFolder.GetFolderFromPathAsync(directoryPath);
        }
        catch (Exception ex)
        {
            LogService.Log($"Failed to access database directory '{directoryPath}': {ex.Message}");
            return;
        }

        foreach (string fileName in new[] { Path.GetFileName(dbPath), Path.GetFileName(dbPath) + "-shm", Path.GetFileName(dbPath) + "-wal" })
        {
            await TryDeleteStorageItemAsync(folder, fileName);
        }
    }

    // -------------------------------------------------------------------------
    // Schema DDL constants
    // -------------------------------------------------------------------------

    /// <summary>Stores the root folder paths included in each media library scan.</summary>
    private const string CreateLibraryFoldersSql = """
        CREATE TABLE IF NOT EXISTS library_folders (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            path       TEXT    UNIQUE NOT NULL,
            media_type INTEGER        NOT NULL
        );
        """;

    /// <summary>
    /// Cache of media file metadata.  Acts as a pure cache – no foreign-key constraints.
    /// Music-specific and video-specific columns are stored in the same row; unused columns are NULL.
    /// </summary>
    private const string CreateMediaRecordsSql = """
        CREATE TABLE IF NOT EXISTS media_records (
            path          TEXT PRIMARY KEY,
            title         TEXT,
            media_type    INTEGER,
            date_added    INTEGER,
            duration_ticks INTEGER,
            year          INTEGER,
            -- music columns
            artist        TEXT,
            album         TEXT,
            album_artist  TEXT,
            composers     TEXT,
            genre         TEXT,
            track_number  INTEGER,
            bitrate       INTEGER,
            -- video columns
            subtitle      TEXT,
            producers     TEXT,
            writers       TEXT,
            width         INTEGER,
            height        INTEGER,
            video_bitrate INTEGER
        );
        """;

    /// <summary>Persists the last-known playback position for each media file (resume from position).</summary>
    private const string CreatePlaybackProgressSql = """
        CREATE TABLE IF NOT EXISTS playback_progress (
            location      TEXT    PRIMARY KEY,
            position_ticks INTEGER NOT NULL
        );
        """;

    /// <summary>User-created playlist metadata.</summary>
    private const string CreatePlaylistsSql = """
        CREATE TABLE IF NOT EXISTS playlists (
            id           TEXT PRIMARY KEY,
            display_name TEXT    NOT NULL,
            last_updated INTEGER NOT NULL
        );
        """;

    /// <summary>
    /// Items belonging to a playlist. Only path and order are stored; metadata comes from
    /// <c>media_records</c> at query time.
    /// </summary>
    private const string CreatePlaylistItemsSql = """
        CREATE TABLE IF NOT EXISTS playlist_items (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            playlist_id TEXT    NOT NULL REFERENCES playlists(id) ON DELETE CASCADE,
            path        TEXT    NOT NULL,
            sort_order  INTEGER NOT NULL
        );
        """;

    private const string SelectMediaRecordsByTypeSql = """
        SELECT path, title, media_type, date_added, duration_ticks, year,
               artist, album, album_artist, composers, genre, track_number, bitrate,
               subtitle, producers, writers, width, height, video_bitrate
        FROM media_records
        WHERE media_type = @mt;
        """;

    private const string UpsertMusicMediaRecordSql = """
        INSERT OR REPLACE INTO media_records
            (path, title, media_type, date_added, duration_ticks, year,
             artist, album, album_artist, composers, genre, track_number, bitrate)
        VALUES
            (@path, @title, @mt, @dateAdded, @durationTicks, @year,
             @artist, @album, @albumArtist, @composers, @genre, @trackNumber, @bitrate);
        """;

    private const string UpsertVideoMediaRecordSql = """
        INSERT OR REPLACE INTO media_records
            (path, title, media_type, date_added, duration_ticks, year,
             subtitle, producers, writers, width, height, video_bitrate)
        VALUES
            (@path, @title, @mt, @dateAdded, @durationTicks, @year,
             @subtitle, @producers, @writers, @width, @height, @videoBitrate);
        """;

    private const string SelectPlaylistItemsWithMetadataSql = """
        SELECT pi.path,
               mr.title, mr.media_type, mr.date_added, mr.duration_ticks, mr.year,
               mr.artist, mr.album, mr.album_artist, mr.composers, mr.genre, mr.track_number,
               mr.subtitle, mr.producers, mr.writers
        FROM playlist_items pi
        LEFT JOIN media_records mr ON mr.path = pi.path
        WHERE pi.playlist_id = @pid
        ORDER BY pi.sort_order, pi.id;
        """;

}

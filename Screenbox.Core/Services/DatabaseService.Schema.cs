#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Screenbox.Core.Models;
using Windows.Storage;

namespace Screenbox.Core.Services;

public sealed partial class DatabaseService
{
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

        _connectionString = connectionString;

        try
        {
            await EnsureSchemaAsync();
        }
        catch (Exception ex) when (ex is SqliteException or IOException)
        {
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
            PlaylistRecordDto? playlist = await TryReadLegacyPlaylistAsync(playlistFile);
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
                RawMediaRecordDto item = playlist.Items[i];
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

    private static async Task<PlaylistRecordDto?> TryReadLegacyPlaylistAsync(StorageFile playlistFile)
    {
        try
        {
            string json = await FileIO.ReadTextAsync(playlistFile);
            var options = new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<PlaylistRecordDto>(json, options);
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

    private static async Task TryDeleteDatabaseAsync(string dbPath)
    {
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

    private const string CreateLibraryFoldersSql = """
        CREATE TABLE IF NOT EXISTS library_folders (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            path       TEXT    UNIQUE NOT NULL,
            media_type INTEGER        NOT NULL
        );
        """;

    private const string CreateMediaRecordsSql = """
        CREATE TABLE IF NOT EXISTS media_records (
            path          TEXT PRIMARY KEY,
            title         TEXT,
            media_type    INTEGER,
            date_added    INTEGER,
            duration_ticks INTEGER,
            year          INTEGER,
            artist        TEXT,
            album         TEXT,
            album_artist  TEXT,
            composers     TEXT,
            genre         TEXT,
            track_number  INTEGER,
            bitrate       INTEGER,
            subtitle      TEXT,
            producers     TEXT,
            writers       TEXT,
            width         INTEGER,
            height        INTEGER,
            video_bitrate INTEGER
        );
        """;

    private const string CreatePlaybackProgressSql = """
        CREATE TABLE IF NOT EXISTS playback_progress (
            location      TEXT    PRIMARY KEY,
            position_ticks INTEGER NOT NULL
        );
        """;

    private const string CreatePlaylistsSql = """
        CREATE TABLE IF NOT EXISTS playlists (
            id           TEXT PRIMARY KEY,
            display_name TEXT    NOT NULL,
            last_updated INTEGER NOT NULL
        );
        """;

    private const string CreatePlaylistItemsSql = """
        CREATE TABLE IF NOT EXISTS playlist_items (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            playlist_id TEXT    NOT NULL REFERENCES playlists(id) ON DELETE CASCADE,
            path        TEXT    NOT NULL,
            sort_order  INTEGER NOT NULL
        );
        """;
}

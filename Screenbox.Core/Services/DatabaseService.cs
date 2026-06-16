#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
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

    private string? _connectionString;
    private readonly object _initLock = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Off-load the synchronous file/SQLite work from the calling (UI) thread.
        await Task.Run(EnsureInitialized);
    }

    /// <inheritdoc/>
    public SqliteConnection CreateConnection()
    {
        // Lazily initialize so callers that skip InitializeAsync still work.
        EnsureInitialized();

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

    private void EnsureInitialized()
    {
        if (_connectionString is not null) return;

        lock (_initLock)
        {
            if (_connectionString is not null) return;
            InitializeCore();
        }
    }

    private void InitializeCore()
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
            EnsureSchema();
        }
        catch (Exception ex) when (ex is SqliteException or IOException)
        {
            // Last resort: if recovery cannot succeed, recreate the database file.
            LogService.Log($"Database schema recovery failed; recreating database file.\n{ex}");
            RecreateDatabaseFile(dbPath, connectionString);
        }
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        ExecuteNonQuery(connection, "PRAGMA journal_mode=WAL;");
        ExecuteNonQuery(connection, "PRAGMA foreign_keys=OFF;");

        using var transaction = connection.BeginTransaction();
        EnsureReplaceableTable(connection, "library_folders", CreateLibraryFoldersSql, "id", "path", "media_type");
        EnsureReplaceableTable(connection, "media_records", CreateMediaRecordsSql,
            "path", "title", "media_type", "date_added", "duration_ticks", "year",
            "artist", "album", "album_artist", "composers", "genre", "track_number", "bitrate",
            "subtitle", "producers", "writers", "width", "height", "video_bitrate");
        EnsureReplaceableTable(connection, "playback_progress", CreatePlaybackProgressSql, "location", "position_ticks");
        EnsurePlaylistsTable(connection);
        EnsurePlaylistItemsTable(connection);
        transaction.Commit();
        ExecuteNonQuery(connection, "PRAGMA foreign_keys=ON;");
    }

    private void RecreateDatabaseFile(string dbPath, string connectionString)
    {
        _connectionString = null;
        TryDeleteDatabase(dbPath);
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

        ExecuteNonQuery(connection, "DROP TABLE IF EXISTS playlists_new;");
        ExecuteNonQuery(connection, CreatePlaylistsMigrationSql);

        string idProjection = actualColumns.Contains("id") ? "id" : "lower(hex(randomblob(16)))";
        string nameProjection = actualColumns.Contains("display_name") ? "display_name" : "''";
        string nowTicks = DateTimeOffset.UtcNow.UtcTicks.ToString();
        string updatedProjection = actualColumns.Contains("last_updated") ? "last_updated" : nowTicks;
        ExecuteNonQuery(connection, $"""
            INSERT OR REPLACE INTO playlists_new (id, display_name, last_updated)
            SELECT {idProjection}, {nameProjection}, {updatedProjection}
            FROM playlists
            WHERE {idProjection} IS NOT NULL;
            """);

        ExecuteNonQuery(connection, "DROP TABLE playlists;");
        ExecuteNonQuery(connection, "ALTER TABLE playlists_new RENAME TO playlists;");
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

        ExecuteNonQuery(connection, "DROP TABLE IF EXISTS playlist_items_new;");
        ExecuteNonQuery(connection, CreatePlaylistItemsMigrationSql);

        string playlistIdProjection = actualColumns.Contains("playlist_id") ? "playlist_id" : "NULL";
        string pathProjection = actualColumns.Contains("path") ? "path" : "NULL";
        string sortOrderProjection = actualColumns.Contains("sort_order")
            ? "sort_order"
            : actualColumns.Contains("id") ? "id" : "rowid";

        ExecuteNonQuery(connection, $"""
            INSERT INTO playlist_items_new (playlist_id, path, sort_order)
            SELECT {playlistIdProjection}, {pathProjection}, {sortOrderProjection}
            FROM playlist_items
            WHERE {playlistIdProjection} IS NOT NULL
              AND {pathProjection} IS NOT NULL
              AND EXISTS (
                  SELECT 1
                  FROM playlists
                  WHERE playlists.id = {playlistIdProjection}
              );
            """);

        ExecuteNonQuery(connection, "DROP TABLE playlist_items;");
        ExecuteNonQuery(connection, "ALTER TABLE playlist_items_new RENAME TO playlist_items;");
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

    private static void TryDeleteDatabase(string dbPath)
    {
        // Also remove WAL and shared-memory sidecar files.
        foreach (string path in new[] { dbPath, dbPath + "-shm", dbPath + "-wal" })
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                LogService.Log($"Failed to delete '{path}': {ex.Message}");
            }
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

    private const string CreatePlaylistsMigrationSql = """
        CREATE TABLE playlists_new (
            id           TEXT PRIMARY KEY,
            display_name TEXT    NOT NULL,
            last_updated INTEGER NOT NULL
        );
        """;

    private const string CreatePlaylistItemsMigrationSql = """
        CREATE TABLE playlist_items_new (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            playlist_id TEXT    NOT NULL REFERENCES playlists(id) ON DELETE CASCADE,
            path        TEXT    NOT NULL,
            sort_order  INTEGER NOT NULL
        );
        """;
}

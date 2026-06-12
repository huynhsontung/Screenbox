#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="IDatabaseService"/> using a single SQLite file stored in
/// <see cref="ApplicationData.LocalFolder"/>.
/// The database is treated as a disposable cache: if corruption is detected the file is
/// deleted and the schema is recreated from scratch with no data.
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
            CreateSchema();
        }
        catch (SqliteException ex)
        {
            // Database file is corrupted. Remove it and start fresh.
            LogService.Log($"Database corrupted; recreating.\n{ex}");
            _connectionString = null;
            TryDeleteDatabase(dbPath);
            _connectionString = connectionString;
            CreateSchema();
        }
    }

    private void CreateSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // WAL mode is a database-level, persistent setting that allows concurrent reads
        // while a write is in progress.
        ExecuteNonQuery(connection, "PRAGMA journal_mode=WAL;");

        using var transaction = connection.BeginTransaction();
        ExecuteNonQuery(connection, CreateLibraryFoldersSql);
        ExecuteNonQuery(connection, CreateMediaRecordsSql);
        ExecuteNonQuery(connection, CreatePlaybackProgressSql);
        ExecuteNonQuery(connection, CreatePlaylistsSql);
        ExecuteNonQuery(connection, CreatePlaylistItemsSql);
        transaction.Commit();
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
    /// Items belonging to a playlist.  A snapshot of media metadata is embedded at insertion time
    /// so that playlist items display correctly even when the source file has moved.
    /// </summary>
    private const string CreatePlaylistItemsSql = """
        CREATE TABLE IF NOT EXISTS playlist_items (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            playlist_id TEXT    NOT NULL REFERENCES playlists(id) ON DELETE CASCADE,
            path        TEXT    NOT NULL,
            title       TEXT,
            media_type  INTEGER,
            date_added  INTEGER,
            duration_ticks INTEGER,
            year        INTEGER,
            -- music columns
            artist      TEXT,
            album       TEXT,
            album_artist TEXT,
            composers   TEXT,
            genre       TEXT,
            track_number INTEGER,
            -- video columns
            subtitle    TEXT,
            producers   TEXT,
            writers     TEXT,
            sort_order  INTEGER NOT NULL
        );
        """;
}

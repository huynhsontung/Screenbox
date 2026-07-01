#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="IDatabaseService"/> using a single SQLite file stored in
/// <see cref="Windows.Storage.ApplicationData.LocalFolder"/>.
/// </summary>
public sealed partial class DatabaseService : IDatabaseService
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

    private SqliteConnection CreateConnection()
    {
        if (_connectionString is null)
        {
            throw new InvalidOperationException("DatabaseService is not initialized. Call InitializeAsync before creating connections.");
        }

        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();

        return connection;
    }
}

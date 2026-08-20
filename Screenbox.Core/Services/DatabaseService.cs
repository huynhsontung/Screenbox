using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="IDatabaseService"/> using a single SQLite file stored in
/// <see cref="ApplicationData.LocalFolder"/>.
/// </summary>
public sealed partial class DatabaseService : IDatabaseService
{
    private const string DbFileName = "screenbox.db";
    private const string LegacyPlaylistsFolderName = "Playlists";
    private static readonly string[] LegacyLocalFileNames = ["songs.bin", "videos.bin"];

    /// <summary>
    /// Gets the folder path where the database and migration files are stored.
    /// Defaults to <c>ApplicationData.Current.LocalFolder.Path</c> if not specified.
    /// </summary>
    public string DbFolderPath { get; set; } = string.Empty;

    private string? _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private readonly object _initLock = new();
    private Task? _initializationTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
    }

    private static string GetUwpFolderPath()
    {
        return ApplicationData.Current.LocalFolder.Path;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(DbFolderPath))
        {
            DbFolderPath = GetUwpFolderPath();
        }

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

#nullable enable

using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Screenbox.Core.Services;

/// <summary>
/// Manages the application's SQLite database.
/// The database acts as a quick cache layer; data loss is handled gracefully by recreating the database.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Initializes the database, creating it if necessary and applying the schema.
    /// If corruption is detected, the database file is deleted and recreated from scratch.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Opens and returns a new <see cref="SqliteConnection"/> to the database.
    /// The caller is responsible for disposing the returned connection.
    /// </summary>
    /// <remarks>
    /// <see cref="InitializeAsync"/> must be called before the first use of this method.
    /// Each connection has foreign-key enforcement enabled automatically.
    /// </remarks>
    SqliteConnection CreateConnection();
}

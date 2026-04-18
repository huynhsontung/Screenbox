#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Screenbox.Core.Data;
using Screenbox.Core.Enums;
using Windows.Storage;

namespace Screenbox.Core.Services;

/// <summary>
/// SQLite-backed implementation of <see cref="IScreenboxDatabase"/>.
/// The database is treated purely as a cache — data loss is handled gracefully
/// by deleting the corrupt file and creating a fresh schema.
/// </summary>
internal sealed class ScreenboxDatabase : IScreenboxDatabase
{
    private const string DbFileName = "screenbox.db";

    // Legacy Protobuf bin files that should be removed on first run
    private const string LegacySongsCacheFileName = "songs.bin";
    private const string LegacyVideosCacheFileName = "videos.bin";
    private const string LegacyLastPositionsFileName = "last_positions.bin";

    private readonly DbContextOptions<ScreenboxDbContext> _dbOptions;

    public ScreenboxDatabase(DbContextOptions<ScreenboxDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Delete legacy .bin cache files — no migration, just cleanup
        await DeleteLegacyBinFilesAsync();

        try
        {
            using var context = new ScreenboxDbContext(_dbOptions);
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception)
        {
            // DB is corrupt or the schema is incompatible — discard and recreate
            await DeleteDatabaseFileAsync();
            using var freshContext = new ScreenboxDbContext(_dbOptions);
            await freshContext.Database.EnsureCreatedAsync();
        }
    }

    // ── Library cache ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task SaveLibraryCacheAsync(
        MediaPlaybackType libraryType,
        IEnumerable<MediaRecordEntity> records,
        IEnumerable<string> folderPaths)
    {
        using var context = new ScreenboxDbContext(_dbOptions);

        // Replace existing records and folder entries for this library type
        var existingRecords = await context.MediaRecords
            .Where(r => r.LibraryType == libraryType)
            .ToListAsync();
        context.MediaRecords.RemoveRange(existingRecords);

        var existingFolders = await context.LibraryFolders
            .Where(f => f.LibraryType == libraryType)
            .ToListAsync();
        context.LibraryFolders.RemoveRange(existingFolders);

        foreach (var record in records)
        {
            record.LibraryType = libraryType;
            context.MediaRecords.Add(record);
        }

        foreach (var path in folderPaths)
        {
            context.LibraryFolders.Add(new LibraryFolderEntity
            {
                LibraryType = libraryType,
                Path = path
            });
        }

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(List<MediaRecordEntity> Records, List<string> FolderPaths)> LoadLibraryCacheAsync(
        MediaPlaybackType libraryType)
    {
        using var context = new ScreenboxDbContext(_dbOptions);

        var records = await context.MediaRecords
            .Where(r => r.LibraryType == libraryType)
            .ToListAsync();

        var folderPaths = await context.LibraryFolders
            .Where(f => f.LibraryType == libraryType)
            .Select(f => f.Path)
            .ToListAsync();

        return (records, folderPaths);
    }

    /// <inheritdoc />
    public async Task ClearLibraryCacheAsync(MediaPlaybackType libraryType)
    {
        using var context = new ScreenboxDbContext(_dbOptions);

        var records = await context.MediaRecords
            .Where(r => r.LibraryType == libraryType)
            .ToListAsync();
        context.MediaRecords.RemoveRange(records);

        var folders = await context.LibraryFolders
            .Where(f => f.LibraryType == libraryType)
            .ToListAsync();
        context.LibraryFolders.RemoveRange(folders);

        await context.SaveChangesAsync();
    }

    // ── Playback progress ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<List<PlaybackProgressEntity>> GetAllPlaybackProgressesAsync()
    {
        using var context = new ScreenboxDbContext(_dbOptions);
        return await context.PlaybackProgresses
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task SaveAllPlaybackProgressesAsync(IEnumerable<PlaybackProgressEntity> items)
    {
        using var context = new ScreenboxDbContext(_dbOptions);

        var existing = await context.PlaybackProgresses.ToListAsync();
        context.PlaybackProgresses.RemoveRange(existing);

        var list = items.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            list[i].SortOrder = i;
            list[i].Id = 0;  // Let the DB assign a new Id
            context.PlaybackProgresses.Add(list[i]);
        }

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemovePlaybackProgressAsync(string location)
    {
        using var context = new ScreenboxDbContext(_dbOptions);
        var entry = await context.PlaybackProgresses
            .FirstOrDefaultAsync(p => p.Location == location);
        if (entry != null)
        {
            context.PlaybackProgresses.Remove(entry);
            await context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task ClearPlaybackProgressesAsync()
    {
        using var context = new ScreenboxDbContext(_dbOptions);
        var all = await context.PlaybackProgresses.ToListAsync();
        context.PlaybackProgresses.RemoveRange(all);
        await context.SaveChangesAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task DeleteLegacyBinFilesAsync()
    {
        // LocalFolder: songs.bin and videos.bin
        await TryDeleteFileAsync(ApplicationData.Current.LocalFolder, LegacySongsCacheFileName);
        await TryDeleteFileAsync(ApplicationData.Current.LocalFolder, LegacyVideosCacheFileName);
        // TemporaryFolder: last_positions.bin
        await TryDeleteFileAsync(ApplicationData.Current.TemporaryFolder, LegacyLastPositionsFileName);
    }

    private static async Task TryDeleteFileAsync(StorageFolder folder, string fileName)
    {
        try
        {
            var file = await folder.GetFileAsync(fileName);
            await file.DeleteAsync();
        }
        catch (FileNotFoundException)
        {
            // File does not exist — nothing to delete
        }
        catch (Exception)
        {
            // Ignore other errors; cleanup is best-effort
        }
    }

    private static async Task DeleteDatabaseFileAsync()
    {
        await TryDeleteFileAsync(ApplicationData.Current.LocalFolder, DbFileName);
    }
}

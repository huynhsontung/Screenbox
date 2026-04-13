#nullable enable

using Microsoft.EntityFrameworkCore;

namespace Screenbox.Core.Data;

/// <summary>
/// EF Core database context for the Screenbox SQLite cache.
/// Used as a quick cache layer for the media library and playback progress;
/// not a source of truth. The app recrawls the disk whenever the cache is missing
/// or corrupt.
/// </summary>
internal class ScreenboxDbContext : DbContext
{
    public DbSet<MediaRecordEntity> MediaRecords { get; set; } = null!;

    public DbSet<LibraryFolderEntity> LibraryFolders { get; set; } = null!;

    public DbSet<PlaybackProgressEntity> PlaybackProgresses { get; set; } = null!;

    public ScreenboxDbContext(DbContextOptions<ScreenboxDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique index on (LibraryType, Path) — each file appears once per library type
        modelBuilder.Entity<MediaRecordEntity>()
            .HasIndex(r => new { r.LibraryType, r.Path })
            .IsUnique();

        // Unique index on Location — one progress entry per media item
        modelBuilder.Entity<PlaybackProgressEntity>()
            .HasIndex(p => p.Location)
            .IsUnique();
    }
}

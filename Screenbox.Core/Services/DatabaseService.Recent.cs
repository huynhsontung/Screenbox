using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public sealed partial class DatabaseService
{
    /// <inheritdoc/>
    public async Task<List<RecentEntryDto>> LoadRecentEntriesAsync(int maxEntries = 100)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();

        var entries = new List<RecentEntryDto>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT id, kind, source_id, title, subtitle, content_hash, initial_index, last_played
                FROM recent_entries
                ORDER BY last_played DESC
                LIMIT @max;
                """;
            cmd.Parameters.AddWithValue("@max", maxEntries);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new RecentEntryDto
                {
                    Id = reader.GetString(0),
                    Kind = (RecentItemKind)reader.GetInt32(1),
                    SourceId = reader.GetString(2),
                    Title = reader.GetString(3),
                    Subtitle = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ContentHash = reader.GetString(5),
                    InitialIndex = reader.GetInt32(6),
                    LastPlayed = new DateTimeOffset(reader.GetInt64(7), TimeSpan.Zero),
                });
            }
        }

        foreach (RecentEntryDto entry in entries)
        {
            if (entry.Kind is RecentItemKind.SingleMedia or RecentItemKind.MediaCollection)
            {
                entry.Items = ReadRecentEntryItems(connection, entry.Id);
            }
        }

        return entries;
    }

    /// <inheritdoc/>
    public async Task UpsertRecentEntryAsync(RecentEntryDto entry, string? replaceEntryId = null, int maxEntries = 100)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        if (!string.IsNullOrEmpty(replaceEntryId) && !string.Equals(replaceEntryId, entry.Id, StringComparison.Ordinal))
        {
            using var deleteReplacedCmd = connection.CreateCommand();
            deleteReplacedCmd.CommandText = "DELETE FROM recent_entries WHERE id = @replaceId;";
            deleteReplacedCmd.Parameters.AddWithValue("@replaceId", replaceEntryId);
            deleteReplacedCmd.ExecuteNonQuery();
        }

        // Remove any existing duplicate row with the same content_hash if its id differs
        using (var deleteDuplicateCmd = connection.CreateCommand())
        {
            deleteDuplicateCmd.CommandText = "DELETE FROM recent_entries WHERE content_hash = @hash AND id <> @id;";
            deleteDuplicateCmd.Parameters.AddWithValue("@hash", entry.ContentHash);
            deleteDuplicateCmd.Parameters.AddWithValue("@id", entry.Id);
            deleteDuplicateCmd.ExecuteNonQuery();
        }

        using (var upsertCmd = connection.CreateCommand())
        {
            upsertCmd.CommandText = """
                INSERT OR REPLACE INTO recent_entries
                    (id, kind, source_id, title, subtitle, content_hash, initial_index, last_played)
                VALUES
                    (@id, @kind, @sourceId, @title, @subtitle, @hash, @initialIndex, @lastPlayed);
                """;
            upsertCmd.Parameters.AddWithValue("@id", entry.Id);
            upsertCmd.Parameters.AddWithValue("@kind", (int)entry.Kind);
            upsertCmd.Parameters.AddWithValue("@sourceId", entry.SourceId);
            upsertCmd.Parameters.AddWithValue("@title", entry.Title);
            upsertCmd.Parameters.AddWithValue("@subtitle", (object?)entry.Subtitle ?? DBNull.Value);
            upsertCmd.Parameters.AddWithValue("@hash", entry.ContentHash);
            upsertCmd.Parameters.AddWithValue("@initialIndex", entry.InitialIndex);
            upsertCmd.Parameters.AddWithValue("@lastPlayed", entry.LastPlayed.UtcTicks);
            upsertCmd.ExecuteNonQuery();
        }

        using (var clearItemsCmd = connection.CreateCommand())
        {
            clearItemsCmd.CommandText = "DELETE FROM recent_entry_items WHERE entry_id = @id;";
            clearItemsCmd.Parameters.AddWithValue("@id", entry.Id);
            clearItemsCmd.ExecuteNonQuery();
        }

        if (entry.Kind is RecentItemKind.SingleMedia or RecentItemKind.MediaCollection && entry.Items.Count > 0)
        {
            using var insertItemCmd = connection.CreateCommand();
            insertItemCmd.CommandText = """
                INSERT INTO recent_entry_items
                    (entry_id, sort_order, path, title, media_type, duration_ticks)
                VALUES
                    (@eid, @order, @path, @title, @mediaType, @duration);
                """;

            var p = insertItemCmd.Parameters;
            p.AddWithValue("@eid", entry.Id);
            var pOrder = p.Add("@order", SqliteType.Integer);
            var pPath = p.Add("@path", SqliteType.Text);
            var pTitle = p.Add("@title", SqliteType.Text);
            var pMediaType = p.Add("@mediaType", SqliteType.Integer);
            var pDuration = p.Add("@duration", SqliteType.Integer);

            for (int i = 0; i < entry.Items.Count; i++)
            {
                RawMediaRecordDto item = entry.Items[i];
                pOrder.Value = i;
                pPath.Value = item.Path;
                pTitle.Value = string.IsNullOrEmpty(item.Title) ? DBNull.Value : item.Title;
                pMediaType.Value = (int)item.MediaType;
                pDuration.Value = item.Duration.Ticks;
                insertItemCmd.ExecuteNonQuery();
            }
        }

        if (maxEntries > 0)
        {
            using var pruneCmd = connection.CreateCommand();
            pruneCmd.CommandText = """
                DELETE FROM recent_entries
                WHERE id NOT IN (
                    SELECT id FROM recent_entries
                    ORDER BY last_played DESC
                    LIMIT @max
                );
                """;
            pruneCmd.Parameters.AddWithValue("@max", maxEntries);
            pruneCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <inheritdoc/>
    public async Task DeleteRecentEntryAsync(string id)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM recent_entries WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public async Task ClearRecentEntriesAsync()
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        ExecuteNonQuery(connection, "DELETE FROM recent_entry_items;");
        ExecuteNonQuery(connection, "DELETE FROM recent_entries;");
        transaction.Commit();
    }

    private static List<RawMediaRecordDto> ReadRecentEntryItems(SqliteConnection connection, string entryId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = SelectRecentEntryItemsWithMetadataSql;
        cmd.Parameters.AddWithValue("@eid", entryId);

        var items = new List<RawMediaRecordDto>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string path = reader.GetString(0);
            string? savedTitle = reader.IsDBNull(1) ? null : reader.GetString(1);
            int savedMediaType = reader.IsDBNull(2) ? (int)MediaPlaybackType.Unknown : reader.GetInt32(2);
            long savedDurationTicks = reader.IsDBNull(3) ? 0L : reader.GetInt64(3);

            string fallbackTitle = !string.IsNullOrWhiteSpace(savedTitle)
                ? savedTitle!
                : Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(fallbackTitle))
            {
                fallbackTitle = path;
            }

            items.Add(new RawMediaRecordDto
            {
                Path = path,
                Title = reader.IsDBNull(4) ? fallbackTitle : reader.GetString(4),
                MediaType = (MediaPlaybackType)(reader.IsDBNull(5) ? savedMediaType : reader.GetInt32(5)),
                DateAdded = reader.IsDBNull(6) ? default : new DateTimeOffset(reader.GetInt64(6), TimeSpan.Zero),
                Duration = reader.IsDBNull(7) ? TimeSpan.FromTicks(savedDurationTicks) : TimeSpan.FromTicks(reader.GetInt64(7)),
                Year = reader.IsDBNull(8) ? 0u : (uint)reader.GetInt64(8),
                Artist = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Album = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                AlbumArtist = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                Composers = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                Genre = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                TrackNumber = reader.IsDBNull(14) ? 0u : (uint)reader.GetInt64(14),
                Subtitle = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                Producers = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                Writers = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                Bitrate = reader.IsDBNull(18) ? 0u : (uint)reader.GetInt64(18),
                Width = reader.IsDBNull(19) ? 0u : (uint)reader.GetInt64(19),
                Height = reader.IsDBNull(20) ? 0u : (uint)reader.GetInt64(20),
                VideoBitrate = reader.IsDBNull(21) ? 0u : (uint)reader.GetInt64(21),
            });
        }

        return items;
    }

    private const string SelectRecentEntryItemsWithMetadataSql = """
        SELECT rei.path, rei.title, rei.media_type, rei.duration_ticks,
               mr.title, mr.media_type, mr.date_added, mr.duration_ticks, mr.year,
               mr.artist, mr.album, mr.album_artist, mr.composers, mr.genre, mr.track_number,
               mr.subtitle, mr.producers, mr.writers, mr.bitrate, mr.width, mr.height, mr.video_bitrate
        FROM recent_entry_items rei
        LEFT JOIN media_records mr ON mr.path = rei.path
        WHERE rei.entry_id = @eid
        ORDER BY rei.sort_order, rei.id;
        """;
}

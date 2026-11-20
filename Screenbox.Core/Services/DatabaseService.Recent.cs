using System;
using System.Collections.Generic;
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
        using var cmd = connection.CreateCommand();
        cmd.CommandText = SelectRecentEntriesWithMetadataSql;
        cmd.Parameters.AddWithValue("@singleKind", (int)RecentItemKind.SingleMedia);
        cmd.Parameters.AddWithValue("@max", maxEntries);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string title = reader.GetString(3);
            var entry = new RecentEntryDto
            {
                Id = reader.GetString(0),
                Kind = (RecentItemKind)reader.GetInt32(1),
                SourceId = reader.GetString(2),
                Title = title,
                Subtitle = reader.IsDBNull(4) ? null : reader.GetString(4),
                Token = reader.IsDBNull(5) ? null : reader.GetString(5),
                LastPlayed = new DateTimeOffset(reader.GetInt64(6), TimeSpan.Zero),
            };

            if (!reader.IsDBNull(7))
            {
                entry.MediaRecord = new RawMediaRecordDto
                {
                    Path = reader.GetString(7),
                    Title = reader.IsDBNull(8) ? title : reader.GetString(8),
                    MediaType = (MediaPlaybackType)(reader.IsDBNull(9) ? (int)MediaPlaybackType.Unknown : reader.GetInt32(9)),
                    DateAdded = reader.IsDBNull(10) ? default : new DateTimeOffset(reader.GetInt64(10), TimeSpan.Zero),
                    Duration = reader.IsDBNull(11) ? default : TimeSpan.FromTicks(reader.GetInt64(11)),
                    Year = reader.IsDBNull(12) ? 0u : (uint)reader.GetInt64(12),
                    Artist = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    Album = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                    AlbumArtist = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                    Composers = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                    Genre = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                    TrackNumber = reader.IsDBNull(18) ? 0u : (uint)reader.GetInt64(18),
                    Subtitle = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                    Producers = reader.IsDBNull(20) ? string.Empty : reader.GetString(20),
                    Writers = reader.IsDBNull(21) ? string.Empty : reader.GetString(21),
                    Bitrate = reader.IsDBNull(22) ? 0u : (uint)reader.GetInt64(22),
                    Width = reader.IsDBNull(23) ? 0u : (uint)reader.GetInt64(23),
                    Height = reader.IsDBNull(24) ? 0u : (uint)reader.GetInt64(24),
                    VideoBitrate = reader.IsDBNull(25) ? 0u : (uint)reader.GetInt64(25),
                };
            }

            entries.Add(entry);
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

        using (var upsertCmd = connection.CreateCommand())
        {
            upsertCmd.CommandText = """
                INSERT OR REPLACE INTO recent_entries
                    (id, kind, source_id, title, subtitle, token, last_played)
                VALUES
                    (@id, @kind, @sourceId, @title, @subtitle, @token, @lastPlayed);
                """;
            upsertCmd.Parameters.AddWithValue("@id", entry.Id);
            upsertCmd.Parameters.AddWithValue("@kind", (int)entry.Kind);
            upsertCmd.Parameters.AddWithValue("@sourceId", entry.SourceId);
            upsertCmd.Parameters.AddWithValue("@title", entry.Title);
            upsertCmd.Parameters.AddWithValue("@subtitle", (object?)entry.Subtitle ?? DBNull.Value);
            upsertCmd.Parameters.AddWithValue("@token", (object?)entry.Token ?? DBNull.Value);
            upsertCmd.Parameters.AddWithValue("@lastPlayed", entry.LastPlayed.UtcTicks);
            upsertCmd.ExecuteNonQuery();
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
        ExecuteNonQuery(connection, "DELETE FROM recent_entries;");
    }

    private const string SelectRecentEntriesWithMetadataSql = """
        SELECT re.id, re.kind, re.source_id, re.title, re.subtitle, re.token, re.last_played,
               mr.path, mr.title, mr.media_type, mr.date_added, mr.duration_ticks, mr.year,
               mr.artist, mr.album, mr.album_artist, mr.composers, mr.genre, mr.track_number,
               mr.subtitle, mr.producers, mr.writers, mr.bitrate, mr.width, mr.height, mr.video_bitrate
        FROM recent_entries re
        LEFT JOIN media_records mr ON re.kind = @singleKind AND mr.path = re.source_id
        ORDER BY re.last_played DESC
        LIMIT @max;
        """;
}

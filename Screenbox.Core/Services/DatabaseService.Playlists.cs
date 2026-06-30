#nullable enable

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
    public async Task SavePlaylistAsync(PersistentPlaylist playlist)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                INSERT OR REPLACE INTO playlists (id, display_name, last_updated)
                VALUES (@id, @name, @updated);
                """;
            cmd.Parameters.AddWithValue("@id", playlist.Id);
            cmd.Parameters.AddWithValue("@name", playlist.DisplayName);
            cmd.Parameters.AddWithValue("@updated", playlist.LastUpdated.UtcTicks);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM playlist_items WHERE playlist_id = @id;";
            cmd.Parameters.AddWithValue("@id", playlist.Id);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO playlist_items
                    (playlist_id, path, sort_order)
                VALUES
                    (@pid, @path, @order);
                """;

            var p = cmd.Parameters;
            p.AddWithValue("@pid", playlist.Id);
            var pPath = p.Add("@path", SqliteType.Text);
            var pOrder = p.Add("@order", SqliteType.Integer);

            for (int i = 0; i < playlist.Items.Count; i++)
            {
                PersistentMediaRecord item = playlist.Items[i];
                pPath.Value = item.Path;
                pOrder.Value = i;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    /// <inheritdoc/>
    public async Task<PersistentPlaylist?> LoadPlaylistAsync(string id)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        PersistentPlaylist? playlist = null;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id, display_name, last_updated FROM playlists WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                playlist = new PersistentPlaylist
                {
                    Id = reader.GetString(0),
                    DisplayName = reader.GetString(1),
                    LastUpdated = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero),
                };
            }
        }

        if (playlist is null)
        {
            return null;
        }

        playlist.Items = ReadPlaylistItems(connection, id);
        return playlist;
    }

    /// <inheritdoc/>
    public async Task<List<PersistentPlaylist>> ListPlaylistsAsync()
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();

        var playlists = new List<PersistentPlaylist>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT id, display_name, last_updated FROM playlists ORDER BY last_updated DESC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                playlists.Add(new PersistentPlaylist
                {
                    Id = reader.GetString(0),
                    DisplayName = reader.GetString(1),
                    LastUpdated = new DateTimeOffset(reader.GetInt64(2), TimeSpan.Zero),
                });
            }
        }

        foreach (PersistentPlaylist playlist in playlists)
        {
            playlist.Items = ReadPlaylistItems(connection, playlist.Id);
        }

        return playlists;
    }

    /// <inheritdoc/>
    public async Task DeletePlaylistAsync(string id)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM playlists WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static List<PersistentMediaRecord> ReadPlaylistItems(SqliteConnection connection, string playlistId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = SelectPlaylistItemsWithMetadataSql;
        cmd.Parameters.AddWithValue("@pid", playlistId);

        var items = new List<PersistentMediaRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string path = reader.GetString(0);
            string fallbackTitle = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(fallbackTitle))
            {
                fallbackTitle = path;
            }

            string title = reader.IsDBNull(1) ? fallbackTitle : reader.GetString(1);
            var mediaType = (MediaPlaybackType)(reader.IsDBNull(2) ? (int)MediaPlaybackType.Unknown : reader.GetInt32(2));
            DateTime dateAdded = reader.IsDBNull(3) ? default : new DateTime(reader.GetInt64(3), DateTimeKind.Utc);
            long durationTicks = reader.IsDBNull(4) ? 0L : reader.GetInt64(4);
            uint year = reader.IsDBNull(5) ? 0u : (uint)reader.GetInt64(5);

            IMediaProperties properties;
            if (mediaType == MediaPlaybackType.Music)
            {
                properties = new MusicInfo
                {
                    Title = title,
                    Artist = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Album = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    AlbumArtist = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    Composers = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    Genre = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    TrackNumber = reader.IsDBNull(11) ? 0u : (uint)reader.GetInt64(11),
                    Year = year,
                    Duration = new TimeSpan(durationTicks),
                };
            }
            else
            {
                properties = new VideoInfo
                {
                    Title = title,
                    Subtitle = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    Producers = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    Writers = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                    Year = year,
                    Duration = new TimeSpan(durationTicks),
                };
            }

            items.Add(new PersistentMediaRecord
            {
                Title = title,
                Path = path,
                MediaType = mediaType,
                DateAdded = dateAdded,
                Duration = new TimeSpan(durationTicks),
                Year = year,
                Properties = properties,
            });
        }

        return items;
    }

    private const string SelectPlaylistItemsWithMetadataSql = """
        SELECT pi.path,
               mr.title, mr.media_type, mr.date_added, mr.duration_ticks, mr.year,
               mr.artist, mr.album, mr.album_artist, mr.composers, mr.genre, mr.track_number,
               mr.subtitle, mr.producers, mr.writers
        FROM playlist_items pi
        LEFT JOIN media_records mr ON mr.path = pi.path
        WHERE pi.playlist_id = @pid
        ORDER BY pi.sort_order, pi.id;
        """;
}

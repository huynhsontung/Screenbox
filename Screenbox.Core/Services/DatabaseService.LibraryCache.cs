#nullable enable

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
    public async Task<RawCacheLoadResultDto> LoadLibraryCacheAsync(MediaPlaybackType mediaType)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();

        var folderPaths = new List<string>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT path FROM library_folders WHERE media_type = @mt;";
            cmd.Parameters.AddWithValue("@mt", (int)mediaType);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                folderPaths.Add(reader.GetString(0));
            }
        }

        var records = new List<RawMediaRecordDto>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = SelectMediaRecordsByTypeSql;
            cmd.Parameters.AddWithValue("@mt", (int)mediaType);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(ReadRawRecord(reader));
            }
        }

        return new RawCacheLoadResultDto
        {
            FolderPaths = folderPaths,
            Records = records,
        };
    }

    /// <inheritdoc/>
    public async Task SaveMusicCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<MusicCacheRecordDto> records)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, "DELETE FROM library_folders WHERE media_type = @mt;",
            new SqlParameterDto { Name = "@mt", Value = (int)MediaPlaybackType.Music });

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO library_folders (path, media_type) VALUES (@path, @mt);";
            var pathParam = cmd.Parameters.Add("@path", SqliteType.Text);
            cmd.Parameters.AddWithValue("@mt", (int)MediaPlaybackType.Music);
            foreach (string path in folderPaths)
            {
                pathParam.Value = path;
                cmd.ExecuteNonQuery();
            }
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = UpsertMusicMediaRecordSql;

            var p = cmd.Parameters;
            var pPath = p.Add("@path", SqliteType.Text);
            var pTitle = p.Add("@title", SqliteType.Text);
            p.AddWithValue("@mt", (int)MediaPlaybackType.Music);
            var pDateAdded = p.Add("@dateAdded", SqliteType.Integer);
            var pDuration = p.Add("@durationTicks", SqliteType.Integer);
            var pYear = p.Add("@year", SqliteType.Integer);
            var pArtist = p.Add("@artist", SqliteType.Text);
            var pAlbum = p.Add("@album", SqliteType.Text);
            var pAlbumArtist = p.Add("@albumArtist", SqliteType.Text);
            var pComposers = p.Add("@composers", SqliteType.Text);
            var pGenre = p.Add("@genre", SqliteType.Text);
            var pTrack = p.Add("@trackNumber", SqliteType.Integer);
            var pBitrate = p.Add("@bitrate", SqliteType.Integer);

            foreach (MusicCacheRecordDto record in records)
            {
                pPath.Value = record.Path;
                pTitle.Value = record.Title;
                pDateAdded.Value = record.DateAdded.UtcDateTime.Ticks;
                pDuration.Value = record.Duration.Ticks;
                pYear.Value = (long)record.Year;
                pArtist.Value = record.Artist;
                pAlbum.Value = record.Album;
                pAlbumArtist.Value = record.AlbumArtist;
                pComposers.Value = record.Composers;
                pGenre.Value = record.Genre;
                pTrack.Value = (long)record.TrackNumber;
                pBitrate.Value = (long)record.Bitrate;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    /// <inheritdoc/>
    public async Task SaveVideoCacheAsync(IReadOnlyList<string> folderPaths, IReadOnlyList<VideoCacheRecordDto> records)
    {
        await EnsureInitializedAsync();
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, "DELETE FROM library_folders WHERE media_type = @mt;",
            new SqlParameterDto { Name = "@mt", Value = (int)MediaPlaybackType.Video });

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO library_folders (path, media_type) VALUES (@path, @mt);";
            var pathParam = cmd.Parameters.Add("@path", SqliteType.Text);
            cmd.Parameters.AddWithValue("@mt", (int)MediaPlaybackType.Video);
            foreach (string path in folderPaths)
            {
                pathParam.Value = path;
                cmd.ExecuteNonQuery();
            }
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = UpsertVideoMediaRecordSql;

            var p = cmd.Parameters;
            var pPath = p.Add("@path", SqliteType.Text);
            var pTitle = p.Add("@title", SqliteType.Text);
            p.AddWithValue("@mt", (int)MediaPlaybackType.Video);
            var pDateAdded = p.Add("@dateAdded", SqliteType.Integer);
            var pDuration = p.Add("@durationTicks", SqliteType.Integer);
            var pYear = p.Add("@year", SqliteType.Integer);
            var pSubtitle = p.Add("@subtitle", SqliteType.Text);
            var pProducers = p.Add("@producers", SqliteType.Text);
            var pWriters = p.Add("@writers", SqliteType.Text);
            var pWidth = p.Add("@width", SqliteType.Integer);
            var pHeight = p.Add("@height", SqliteType.Integer);
            var pVideoBitrate = p.Add("@videoBitrate", SqliteType.Integer);

            foreach (VideoCacheRecordDto record in records)
            {
                pPath.Value = record.Path;
                pTitle.Value = record.Title;
                pDateAdded.Value = record.DateAdded.UtcDateTime.Ticks;
                pDuration.Value = record.Duration.Ticks;
                pYear.Value = (long)record.Year;
                pSubtitle.Value = record.Subtitle;
                pProducers.Value = record.Producers;
                pWriters.Value = record.Writers;
                pWidth.Value = (long)record.Width;
                pHeight.Value = (long)record.Height;
                pVideoBitrate.Value = (long)record.VideoBitrate;
                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    private static RawMediaRecordDto ReadRawRecord(SqliteDataReader reader)
    {
        return new RawMediaRecordDto
        {
            Path = reader.GetString(0),
            Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            MediaType = (MediaPlaybackType)(reader.IsDBNull(2) ? 0 : reader.GetInt32(2)),
            DateAdded = reader.IsDBNull(3) ? default : new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero),
            Duration = reader.IsDBNull(4) ? TimeSpan.Zero : new TimeSpan(reader.GetInt64(4)),
            Year = reader.IsDBNull(5) ? 0u : (uint)reader.GetInt64(5),
            Artist = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Album = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            AlbumArtist = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            Composers = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            Genre = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            TrackNumber = reader.IsDBNull(11) ? 0u : (uint)reader.GetInt64(11),
            Bitrate = reader.IsDBNull(12) ? 0u : (uint)reader.GetInt64(12),
            Subtitle = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            Producers = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            Writers = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            Width = reader.IsDBNull(16) ? 0u : (uint)reader.GetInt64(16),
            Height = reader.IsDBNull(17) ? 0u : (uint)reader.GetInt64(17),
            VideoBitrate = reader.IsDBNull(18) ? 0u : (uint)reader.GetInt64(18),
        };
    }

    private const string SelectMediaRecordsByTypeSql = """
        SELECT path, title, media_type, date_added, duration_ticks, year,
               artist, album, album_artist, composers, genre, track_number, bitrate,
               subtitle, producers, writers, width, height, video_bitrate
        FROM media_records
        WHERE media_type = @mt;
        """;

    private const string UpsertMusicMediaRecordSql = """
        INSERT OR REPLACE INTO media_records
            (path, title, media_type, date_added, duration_ticks, year,
             artist, album, album_artist, composers, genre, track_number, bitrate)
        VALUES
            (@path, @title, @mt, @dateAdded, @durationTicks, @year,
             @artist, @album, @albumArtist, @composers, @genre, @trackNumber, @bitrate);
        """;

    private const string UpsertVideoMediaRecordSql = """
        INSERT OR REPLACE INTO media_records
            (path, title, media_type, date_added, duration_ticks, year,
             subtitle, producers, writers, width, height, video_bitrate)
        VALUES
            (@path, @title, @mt, @dateAdded, @durationTicks, @year,
             @subtitle, @producers, @writers, @width, @height, @videoBitrate);
        """;
}

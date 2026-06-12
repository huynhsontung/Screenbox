#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Microsoft.Data.Sqlite;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace Screenbox.Core.Services;

public sealed class PlaylistService : IPlaylistService
{
    private const string ThumbnailsFolderName = "Thumbnails";

    private readonly IMediaListFactory _mediaListFactory;
    private readonly IFilesService _filesService;
    private readonly IDatabaseService _databaseService;

    public PlaylistService(IFilesService filesService, IMediaListFactory mediaListFactory, IDatabaseService databaseService)
    {
        _mediaListFactory = mediaListFactory;
        _filesService = filesService;
        _databaseService = databaseService;
    }

    public async Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery, CancellationToken cancellationToken = default)
    {
        var neighboringFiles = await neighboringFilesQuery.GetFilesAsync();
        var result = await _mediaListFactory.TryParseMediaListAsync(neighboringFiles, null, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (result?.Items.Count > 0)
        {
            var currentItem = playlist.CurrentItem;
            if (currentItem != null)
            {
                // Replace the matching item (by location) with the existing CurrentItem to preserve
                // VM identity. Without this, GetOrCreate creates a new VM for the same file that is a
                // different object reference. Playlist uses IndexOf (reference equality) to find
                // CurrentItem in the new list; if it fails, CurrentIndex becomes -1, which causes
                // LoadFromPlaylist to set PlaybackItem to null and call VlcPlayer.Stop() on the UI
                // thread, freezing the app.
                int matchIndex = result.Items.FindIndex(vm =>
                    vm.Location.Equals(currentItem.Location, StringComparison.OrdinalIgnoreCase));
                if (matchIndex >= 0)
                {
                    result.Items[matchIndex] = currentItem;
                    return new Playlist(currentItem, result.Items, playlist);
                }

                // Current item not found in neighboring files (edge case).
                // Return the playlist unchanged to avoid losing the current position.
                return playlist;
            }

            return new Playlist(result.Items, playlist);
        }

        return playlist;
    }

    public Playlist ShufflePlaylist(Playlist playlist, int? preserveIndex = null)
    {
        var shuffleBackup = new ShuffleBackup(new List<MediaViewModel>(playlist.Items));
        var shuffled = new Playlist(playlist)
        {
            ShuffleMode = true,
            ShuffleBackup = shuffleBackup
        };

        var random = new Random();

        if (preserveIndex.HasValue && preserveIndex.Value >= 0 && preserveIndex.Value < shuffled.Items.Count)
        {
            var activeItem = shuffled.Items[preserveIndex.Value];
            shuffled.Items.RemoveAt(preserveIndex.Value);
            Shuffle(shuffled.Items, random);
            shuffled.Items.Insert(0, activeItem);
            shuffled.CurrentIndex = 0;
        }
        else
        {
            Shuffle(shuffled.Items, random);
        }

        return shuffled;
    }

    public Playlist RestoreFromShuffle(Playlist playlist)
    {
        Guard.IsNotNull(playlist.ShuffleBackup, nameof(playlist.ShuffleBackup));
        var shuffleBackup = playlist.ShuffleBackup;
        var backup = new List<MediaViewModel>(shuffleBackup.OriginalPlaylist);

        foreach (var removal in shuffleBackup.Removals)
        {
            backup.Remove(removal);
        }

        return playlist.CurrentItem != null
            ? new Playlist(playlist.CurrentItem, backup)
            : new Playlist(backup);
    }

    public IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5)
    {
        if (currentIndex < 0 || playlistCount == 0) return Array.Empty<int>();

        int startIndex = Math.Max(currentIndex - 2, 0);
        int endIndex = Math.Min(currentIndex + 2, playlistCount - 1);
        var indices = new List<int>();

        for (int i = startIndex; i <= endIndex; i++)
        {
            indices.Add(i);
        }

        // Add wrap-around indices for list repeat mode
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && indices.Count < bufferSize)
        {
            if (startIndex == 0 && endIndex < playlistCount - 1)
            {
                indices.Add(playlistCount - 1);
            }

            if (startIndex > 0 && endIndex == playlistCount - 1)
            {
                indices.Insert(0, 0);
            }
        }

        return indices.AsReadOnly();
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <summary>
    /// Saves a playlist and all its items to the database.
    /// Existing items for the playlist are replaced atomically.
    /// </summary>
    public async Task SavePlaylistAsync(PersistentPlaylist playlist)
    {
        await Task.Run(() => WritePlaylistToDatabase(playlist));
    }

    /// <summary>
    /// Loads a playlist and its items from the database.
    /// Returns <c>null</c> if the playlist is not found.
    /// </summary>
    public async Task<PersistentPlaylist?> LoadPlaylistAsync(string id)
    {
        return await Task.Run(() => ReadPlaylistFromDatabase(id));
    }

    /// <summary>
    /// Lists all persisted playlists, ordered by <c>last_updated</c> descending.
    /// </summary>
    public async Task<IReadOnlyList<PersistentPlaylist>> ListPlaylistsAsync()
    {
        return await Task.Run(ReadAllPlaylistsFromDatabase);
    }

    /// <summary>Deletes a playlist and cascades to its items.</summary>
    public async Task DeletePlaylistAsync(string id)
    {
        await Task.Run(() =>
        {
            using var connection = _databaseService.CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM playlists WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        });
    }

    public async Task SaveThumbnailAsync(string mediaLocation, byte[] imageBytes)
    {
        StorageFolder thumbnailsFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(ThumbnailsFolderName, CreationCollisionOption.OpenIfExists);
        string hash = GetHash(mediaLocation);
        StorageFile file = await thumbnailsFolder.CreateFileAsync(hash + ".png", CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteBytesAsync(file, imageBytes);
    }

    public async Task<StorageFile?> GetThumbnailFileAsync(string mediaLocation)
    {
        StorageFolder thumbnailsFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(ThumbnailsFolderName, CreationCollisionOption.OpenIfExists);
        string hash = GetHash(mediaLocation);
        try
        {
            return await thumbnailsFolder.GetFileAsync(hash + ".png");
        }
        catch
        {
            return null;
        }
    }

    private static string GetHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input.ToLowerInvariant());
        byte[] hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Appends media items to an existing persistent playlist and persists the updated playlist.
    /// </summary>
    public async Task AddToPlaylistAsync(string playlistId, IReadOnlyList<MediaViewModel> items)
    {
        if (string.IsNullOrWhiteSpace(playlistId)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(playlistId));
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (items.Count == 0) return;

        PersistentPlaylist? playlist = await LoadPlaylistAsync(playlistId);
        if (playlist is null)
        {
            throw new InvalidOperationException($"Playlist '{playlistId}' was not found.");
        }

        foreach (MediaViewModel m in items)
        {
            if (m is null) continue;
            IMediaProperties properties = m.MediaType == MediaPlaybackType.Music
                ? m.MediaInfo.MusicProperties
                : m.MediaInfo.VideoProperties;

            playlist.Items.Add(new PersistentMediaRecord(m.Name, m.Location, properties, m.DateAdded));
        }

        playlist.LastUpdated = DateTimeOffset.Now;
        await SavePlaylistAsync(playlist);
    }

    public async Task<IReadOnlyList<MediaViewModel>> ImportPlaylistItemsAsync(StorageFile file)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));
        var mediaList = await _mediaListFactory.ParseMediaListAsync(file);
        return mediaList.Items;
    }

    public async Task ExportPlaylistItemsAsync(IReadOnlyList<MediaViewModel> items, StorageFile file)
    {
        var lines = new List<string>((items.Count * 2) + 1)
        {
            "#EXTM3U"
        };

        foreach (MediaViewModel item in items.Where(x => x.Location.Length > 0 && x.Location != "about:blank"))
        {
            int durationSeconds = item.Duration > TimeSpan.Zero ? (int)Math.Round(item.Duration.TotalSeconds) : -1;
            string title = item.Name;
            string path = Uri.TryCreate(item.Location, UriKind.Absolute, out var uri) ? uri.AbsoluteUri : item.Location;
            lines.Add($"#EXTINF:{durationSeconds},{title}");
            lines.Add(path);
        }

        await FileIO.WriteLinesAsync(file, lines, UnicodeEncoding.Utf8);
    }

    // -------------------------------------------------------------------------
    // Private database helpers
    // -------------------------------------------------------------------------

    private void WritePlaylistToDatabase(PersistentPlaylist playlist)
    {
        using var connection = _databaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();

        // Upsert playlist header
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

        // Delete old items (simpler than diffing)
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM playlist_items WHERE playlist_id = @id;";
            cmd.Parameters.AddWithValue("@id", playlist.Id);
            cmd.ExecuteNonQuery();
        }

        // Insert items with explicit sort order
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO playlist_items
                    (playlist_id, path, title, media_type, date_added, duration_ticks, year,
                     artist, album, album_artist, composers, genre, track_number,
                     subtitle, producers, writers, sort_order)
                VALUES
                    (@pid, @path, @title, @mt, @dateAdded, @dur, @year,
                     @artist, @album, @albumArtist, @composers, @genre, @track,
                     @subtitle, @producers, @writers, @order);
                """;

            var p = cmd.Parameters;
            p.AddWithValue("@pid", playlist.Id);
            var pPath = p.Add("@path", SqliteType.Text);
            var pTitle = p.Add("@title", SqliteType.Text);
            var pMt = p.Add("@mt", SqliteType.Integer);
            var pDateAdded = p.Add("@dateAdded", SqliteType.Integer);
            var pDur = p.Add("@dur", SqliteType.Integer);
            var pYear = p.Add("@year", SqliteType.Integer);
            var pArtist = p.Add("@artist", SqliteType.Text);
            var pAlbum = p.Add("@album", SqliteType.Text);
            var pAlbumArtist = p.Add("@albumArtist", SqliteType.Text);
            var pComposers = p.Add("@composers", SqliteType.Text);
            var pGenre = p.Add("@genre", SqliteType.Text);
            var pTrack = p.Add("@track", SqliteType.Integer);
            var pSubtitle = p.Add("@subtitle", SqliteType.Text);
            var pProducers = p.Add("@producers", SqliteType.Text);
            var pWriters = p.Add("@writers", SqliteType.Text);
            var pOrder = p.Add("@order", SqliteType.Integer);

            for (int i = 0; i < playlist.Items.Count; i++)
            {
                PersistentMediaRecord item = playlist.Items[i];
                pPath.Value = item.Path;
                pTitle.Value = item.Title ?? (object)DBNull.Value;
                pMt.Value = (int)item.MediaType;
                pDateAdded.Value = item.DateAdded != default ? (object)item.DateAdded.Ticks : DBNull.Value;
                pDur.Value = item.Duration.Ticks;
                pYear.Value = (long)item.Year;
                pOrder.Value = i;

                if (item.Properties is MusicInfo music)
                {
                    pArtist.Value = music.Artist;
                    pAlbum.Value = music.Album;
                    pAlbumArtist.Value = music.AlbumArtist;
                    pComposers.Value = music.Composers;
                    pGenre.Value = music.Genre;
                    pTrack.Value = (long)music.TrackNumber;
                    pSubtitle.Value = (object)DBNull.Value;
                    pProducers.Value = (object)DBNull.Value;
                    pWriters.Value = (object)DBNull.Value;
                }
                else if (item.Properties is VideoInfo video)
                {
                    pArtist.Value = (object)DBNull.Value;
                    pAlbum.Value = (object)DBNull.Value;
                    pAlbumArtist.Value = (object)DBNull.Value;
                    pComposers.Value = (object)DBNull.Value;
                    pGenre.Value = (object)DBNull.Value;
                    pTrack.Value = (object)DBNull.Value;
                    pSubtitle.Value = video.Subtitle;
                    pProducers.Value = video.Producers;
                    pWriters.Value = video.Writers;
                }
                else
                {
                    pArtist.Value = (object)DBNull.Value;
                    pAlbum.Value = (object)DBNull.Value;
                    pAlbumArtist.Value = (object)DBNull.Value;
                    pComposers.Value = (object)DBNull.Value;
                    pGenre.Value = (object)DBNull.Value;
                    pTrack.Value = (object)DBNull.Value;
                    pSubtitle.Value = (object)DBNull.Value;
                    pProducers.Value = (object)DBNull.Value;
                    pWriters.Value = (object)DBNull.Value;
                }

                cmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    private PersistentPlaylist? ReadPlaylistFromDatabase(string id)
    {
        using var connection = _databaseService.CreateConnection();

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

        if (playlist is null) return null;

        playlist.Items = ReadPlaylistItems(connection, id);
        return playlist;
    }

    private List<PersistentPlaylist> ReadAllPlaylistsFromDatabase()
    {
        using var connection = _databaseService.CreateConnection();

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

    private static List<PersistentMediaRecord> ReadPlaylistItems(SqliteConnection connection, string playlistId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT path, title, media_type, date_added, duration_ticks, year,
                   artist, album, album_artist, composers, genre, track_number,
                   subtitle, producers, writers
            FROM playlist_items
            WHERE playlist_id = @pid
            ORDER BY sort_order;
            """;
        cmd.Parameters.AddWithValue("@pid", playlistId);

        var items = new List<PersistentMediaRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string path = reader.GetString(0);
            string title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var mediaType = (MediaPlaybackType)(reader.IsDBNull(2) ? 0 : reader.GetInt32(2));
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
}

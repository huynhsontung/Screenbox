#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;

namespace Screenbox.Core.Services;

public sealed class PlaylistService : IPlaylistService
{
    private const string ThumbnailsFolderName = "Thumbnails";

    private readonly IMediaListFactory _mediaListFactory;
    private readonly IDatabaseService _databaseService;

    public PlaylistService(IMediaListFactory mediaListFactory, IDatabaseService databaseService)
    {
        _mediaListFactory = mediaListFactory;
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
        await _databaseService.SavePlaylistAsync(playlist);
    }

    /// <summary>
    /// Loads a playlist and its items from the database.
    /// Returns <c>null</c> if the playlist is not found.
    /// </summary>
    public async Task<PersistentPlaylist?> LoadPlaylistAsync(string id)
    {
        return await _databaseService.LoadPlaylistAsync(id);
    }

    /// <summary>
    /// Lists all persisted playlists, ordered by <c>last_updated</c> descending.
    /// </summary>
    public async Task<IReadOnlyList<PersistentPlaylist>> ListPlaylistsAsync()
    {
        return await _databaseService.ListPlaylistsAsync();
    }

    /// <summary>Deletes a playlist and cascades to its items.</summary>
    public async Task DeletePlaylistAsync(string id)
    {
        await _databaseService.DeletePlaylistAsync(id);
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
        byte[] bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
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
        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");

        foreach (MediaViewModel item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Location) || string.Equals(item.Location, "about:blank", StringComparison.Ordinal))
                continue;

            int durationSeconds = item.Duration > TimeSpan.Zero
                ? (int)Math.Round(item.Duration.TotalSeconds)
                : -1;

            string title = item.Name;
            string path = Uri.TryCreate(item.Location, UriKind.Absolute, out var uri)
                ? uri.AbsoluteUri
                : item.Location;

            sb.AppendLine($"#EXTINF:{durationSeconds},{title}")
              .AppendLine(path);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await FileIO.WriteBytesAsync(file, bytes);
    }

}

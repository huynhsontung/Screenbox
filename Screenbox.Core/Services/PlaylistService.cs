using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;

namespace Screenbox.Core.Services;

public sealed class PlaylistService : IPlaylistService
{
    private readonly IDatabaseService _databaseService;
    private readonly IQueueService _queueService;

    public PlaylistService(IDatabaseService databaseService, IQueueService queueService)
    {
        _databaseService = databaseService;
        _queueService = queueService;
    }

    /// <summary>
    /// Saves a playlist and all its items to the database.
    /// Existing items for the playlist are replaced atomically.
    /// </summary>
    public async Task SavePlaylistAsync(PlaylistRecordDto playlist)
    {
        await _databaseService.SavePlaylistAsync(playlist);
    }

    /// <summary>
    /// Loads a playlist and its items from the database.
    /// Returns <c>null</c> if the playlist is not found.
    /// </summary>
    public async Task<PlaylistRecordDto?> LoadPlaylistAsync(string id)
    {
        return await _databaseService.LoadPlaylistAsync(id);
    }

    /// <summary>
    /// Lists all persisted playlists, ordered by <c>last_updated</c> descending.
    /// </summary>
    public async Task<IReadOnlyList<PlaylistRecordDto>> ListPlaylistsAsync()
    {
        return await _databaseService.ListPlaylistsAsync();
    }

    /// <summary>Deletes a playlist and cascades to its items.</summary>
    public async Task DeletePlaylistAsync(string id)
    {
        await _databaseService.DeletePlaylistAsync(id);
    }

    /// <summary>
    /// Appends media items to an existing persistent playlist and persists the updated playlist.
    /// </summary>
    public async Task AddToPlaylistAsync(string playlistId, IReadOnlyList<MediaViewModel> items)
    {
        if (string.IsNullOrWhiteSpace(playlistId)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(playlistId));
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (items.Count == 0) return;

        PlaylistRecordDto? playlist = await LoadPlaylistAsync(playlistId);
        if (playlist is null)
        {
            throw new InvalidOperationException($"Playlist '{playlistId}' was not found.");
        }

        foreach (MediaViewModel m in items)
        {
            if (m is null) continue;
            playlist.Items.Add(RawMediaRecordDto.FromMediaViewModel(m));
        }

        playlist.LastUpdated = DateTimeOffset.Now;
        await SavePlaylistAsync(playlist);
    }

    public async Task<IReadOnlyList<MediaViewModel>> ImportPlaylistItemsAsync(StorageFile file)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));
        var queue = await _queueService.ParseQueueAsync(file);
        return queue.Items;
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

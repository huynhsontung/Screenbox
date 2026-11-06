using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;

namespace Screenbox.Core.Services;

public sealed class PlaylistService : IPlaylistService
{
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IDatabaseService _databaseService;
    private readonly IFilesService _filesService;

    public PlaylistService(MediaViewModelFactory mediaFactory, IDatabaseService databaseService, IFilesService filesService)
    {
        _mediaFactory = mediaFactory;
        _databaseService = databaseService;
        _filesService = filesService;
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    public async Task<Playlist?> TryCreatePlaylistAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default)
    {
        if (storageItems.Count == 0) return null;

        Playlist? playlist;
        if (storageItems.Count == 1 && storageItems[0] is StorageFile file)
        {
            playlist = await ParsePlaylistAsync(file, cancellationToken);
            if (playlist.Items.Count == 1 && playlist.Origin?.Kind == RecentItemKind.SingleMedia)
            {
                neighboringFilesQuery ??= await _filesService.GetNeighboringFilesQueryAsync(file);
            }
            else if (playlist.Origin?.Kind == RecentItemKind.AdHocPlaylist)
            {
                _filesService.AddToRecent(file);
            }
        }
        else
        {
            if (storageItems.Count == 1 && storageItems[0] is StorageFolder folder)
            {
                _filesService.AddToRecent(folder);
            }

            playlist = await TryParsePlaylistAsync(storageItems, cancellationToken: cancellationToken);
            if (playlist is null) return null;
        }

        playlist.NeighboringFilesQuery = neighboringFilesQuery;
        return playlist;
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async Task<Playlist?> TryParsePlaylistAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFile? playNext = null,
        bool expandPlaylists = true,
        CancellationToken cancellationToken = default)
    {
        var queue = new List<MediaViewModel>();
        var storageItemQueue = storageItems.ToList();
        MediaViewModel? next = null;

        // Max number of items in queue is 10k. Reevaluate if needed.
        for (int i = 0; i < storageItemQueue.Count && queue.Count < 10000; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = storageItemQueue[i];
            switch (item)
            {
                case StorageFile storageFile when storageFile.IsSupported():
                    var vm = _mediaFactory.GetOrCreate(storageFile);
                    bool isPlayNext = playNext is not null && storageFile.IsEqual(playNext);

                    if (vm.Source is IExpandableMediaSource expandable)
                    {
                        if (expandPlaylists || isPlayNext)
                        {
                            var expandedItems = await expandable.ExpandAsync(cancellationToken);
                            if (expandedItems.Count > 0)
                            {
                                var childVms = expandedItems.Select(_mediaFactory.Create).ToList();

                                if (isPlayNext)
                                    next = childVms[0];
                                queue.AddRange(childVms);
                            }
                            else
                            {
                                if (isPlayNext) next = vm;
                                queue.Add(vm);
                            }
                        }
                        // If expandPlaylists is false and not playNext, skip expandable items (e.g. .m3u in folder)
                    }
                    else
                    {
                        if (isPlayNext)
                            next = vm;
                        queue.Add(vm);
                    }
                    break;

                case StorageFolder storageFolder:
                    var subItems = await storageFolder.GetItemsAsync(0, 10000);
                    storageItemQueue.AddRange(subItems);
                    break;
            }
        }

        if (queue.Count == 0)
        {
            return null;
        }

        var playlist = new Playlist(next ?? queue[0], queue);
        if (storageItems.Count == 1 && storageItems[0] is StorageFolder rootFolder)
        {
            string folderTitle = !string.IsNullOrWhiteSpace(rootFolder.DisplayName)
                ? rootFolder.DisplayName
                : rootFolder.Name;
            playlist.Origin = PlaylistOrigin.ForAdHocPlaylist(folderTitle, rootFolder.Path);
        }
        else if (storageItems.Count == 1 && storageItems[0] is StorageFile singleFile && singleFile.IsSupportedPlaylist())
        {
            string title = Path.GetFileNameWithoutExtension(singleFile.Name);
            playlist.Origin = PlaylistOrigin.ForAdHocPlaylist(string.IsNullOrWhiteSpace(title) ? singleFile.Name : title, singleFile.Path);
        }
        else if (queue.Count > 1)
        {
            playlist.Origin = PlaylistOrigin.ForAdHocPlaylist(queue[0].Name);
        }
        else
        {
            playlist.Origin = PlaylistOrigin.ForSingleMedia(queue[0]);
        }

        return playlist;
    }

    public async Task<Playlist> ParsePlaylistAsync(MediaViewModel media, CancellationToken cancellationToken = default)
    {
        if (media.Source is IExpandableMediaSource expandable)
        {
            var items = await expandable.ExpandAsync(cancellationToken);
            if (items.Count > 0)
            {
                var vms = items.Select(_mediaFactory.Create).ToList();
                string title = Path.GetFileNameWithoutExtension(media.Name);
                return new Playlist(0, vms)
                {
                    Origin = PlaylistOrigin.ForAdHocPlaylist(
                        string.IsNullOrWhiteSpace(title) ? media.Name : title,
                        media.Location)
                };
            }
        }

        return new Playlist(media)
        {
            Origin = PlaylistOrigin.ForSingleMedia(media)
        };
    }

    public async Task<Playlist> ParsePlaylistAsync(StorageFile file, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(file);
        return await ParsePlaylistAsync(media, cancellationToken);
    }

    public async Task<Playlist> ParsePlaylistAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(uri);
        return await ParsePlaylistAsync(media, cancellationToken);
    }

    public async Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, CancellationToken cancellationToken = default)
    {
        if (playlist.NeighboringFilesQuery is not { } neighboringFilesQuery)
        {
            return playlist;
        }

        var neighboringFiles = await neighboringFilesQuery.GetFilesAsync();
        var result = await TryParsePlaylistAsync(neighboringFiles, null, expandPlaylists: false, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (result?.Items.Count > 1)
        {
            string folderName = string.Empty;
            string? folderPath = null;
            try
            {
                if (neighboringFilesQuery.Folder is { } parentFolder)
                {
                    folderName = !string.IsNullOrWhiteSpace(parentFolder.DisplayName)
                        ? parentFolder.DisplayName
                        : parentFolder.Name;
                    folderPath = parentFolder.Path;
                    _filesService.AddToRecent(parentFolder);
                }
            }
            catch
            {
                // Best-effort folder inspection
            }

            var currentItem = playlist.CurrentItem;
            if (string.IsNullOrWhiteSpace(folderName) && currentItem is not null)
            {
                folderPath ??= Path.GetDirectoryName(currentItem.Location);
                folderName = !string.IsNullOrWhiteSpace(folderPath)
                    ? Path.GetFileName(folderPath)
                    : currentItem.Name;
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                folderName = result.Items[0].Name;
            }

            var folderOrigin = PlaylistOrigin.ForAdHocPlaylist(folderName, folderPath);

            if (currentItem is not null)
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
                    return new Playlist(currentItem, result.Items, playlist)
                    {
                        Origin = folderOrigin
                    };
                }

                // Current item not found in neighboring files (edge case).
                // Return the playlist unchanged to avoid losing the current position.
                return playlist;
            }

            return new Playlist(result.Items, playlist)
            {
                Origin = folderOrigin
            };
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

        var restored = playlist.CurrentItem != null
            ? new Playlist(playlist.CurrentItem, backup, playlist)
            : new Playlist(backup, playlist);
        restored.ShuffleMode = false;
        restored.ShuffleBackup = null;
        return restored;
    }

    public void UpdateShuffleBackup(Playlist playlist, NotifyCollectionChangedEventArgs e)
    {
        if (playlist.Items.Count <= 1)
        {
            playlist.ShuffleBackup = null;
            return;
        }

        ShuffleBackup? backup = playlist.ShuffleBackup;
        if (!playlist.ShuffleMode || backup is null)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Remove when e.OldItems?.Count > 0:
                foreach (object item in e.OldItems)
                {
                    if (item is MediaViewModel vm)
                    {
                        backup.Removals.Add(vm);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace when e is
            {
                OldItems: { Count: { } oldCount }, NewItems: { Count: { } newCount }
            } && oldCount == newCount && newCount > 0:
                for (int i = 0; i < oldCount; i++)
                {
                    if (e.OldItems[i] is not MediaViewModel oldItem) continue;
                    if (e.NewItems[i] is not MediaViewModel newItem) continue;
                    int backupIndex = backup.OriginalPlaylist.IndexOf(oldItem);
                    if (backupIndex >= 0)
                    {
                        backup.OriginalPlaylist[backupIndex] = newItem;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                foreach (object item in e.NewItems)
                {
                    if (item is MediaViewModel vm && !backup.Removals.Remove(vm))
                    {
                        playlist.ShuffleBackup = null;
                        break;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                playlist.ShuffleBackup = null;
                break;
        }
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
            playlist.Items.Add(ToRawMediaRecord(m));
        }

        playlist.LastUpdated = DateTimeOffset.Now;
        await SavePlaylistAsync(playlist);
    }

    public async Task<IReadOnlyList<MediaViewModel>> ImportPlaylistItemsAsync(StorageFile file)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));
        var playlist = await ParsePlaylistAsync(file);
        return playlist.Items;
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

    private static RawMediaRecordDto ToRawMediaRecord(MediaViewModel media)
    {
        return new RawMediaRecordDto
        {
            Path = media.Location,
            Title = media.Name,
            MediaType = media.MediaType,
            DateAdded = media.DateAdded,
            Duration = media.Duration,
            Year = media.MediaType == MediaPlaybackType.Music
                ? media.MediaInfo.MusicProperties.Year
                : media.MediaInfo.VideoProperties.Year,
            Artist = media.MediaInfo.MusicProperties.Artist,
            Album = media.MediaInfo.MusicProperties.Album,
            AlbumArtist = media.MediaInfo.MusicProperties.AlbumArtist,
            Composers = media.MediaInfo.MusicProperties.Composers,
            Genre = media.MediaInfo.MusicProperties.Genre,
            TrackNumber = media.MediaInfo.MusicProperties.TrackNumber,
            Bitrate = media.MediaInfo.MusicProperties.Bitrate,
            Subtitle = media.MediaInfo.VideoProperties.Subtitle,
            Producers = media.MediaInfo.VideoProperties.Producers,
            Writers = media.MediaInfo.VideoProperties.Writers,
            Width = media.MediaInfo.VideoProperties.Width,
            Height = media.MediaInfo.VideoProperties.Height,
            VideoBitrate = media.MediaInfo.VideoProperties.Bitrate,
        };
    }

}

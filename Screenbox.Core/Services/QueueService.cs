using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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

namespace Screenbox.Core.Services;

public sealed class QueueService : IQueueService
{
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IFilesService _filesService;

    public QueueService(MediaViewModelFactory mediaFactory, IFilesService filesService)
    {
        _mediaFactory = mediaFactory;
        _filesService = filesService;
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    public async Task<QueueSession?> TryCreateQueueAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default)
    {
        if (storageItems.Count == 0) return null;

        QueueSession? session;
        if (storageItems.Count == 1 && storageItems[0] is StorageFile file)
        {
            session = await ParseQueueAsync(file, cancellationToken);
            if (session.Items.Count == 1 && session.Origin?.Kind == RecentItemKind.SingleMedia)
            {
                neighboringFilesQuery ??= await _filesService.GetNeighboringFilesQueryAsync(file);
            }
            else if (session.Origin?.Kind == RecentItemKind.MediaCollection)
            {
                // Register the playlist file (e.g. .m3u/.m3u8) with the Windows Most Recently Used (MRU) list
                // so the app retains persistent file-access permission to reload it across future sessions.
                _filesService.AddToRecent(file);
            }
        }
        else
        {
            if (storageItems.Count == 1 && storageItems[0] is StorageFolder folder)
            {
                // Register the opened folder with the Windows Most Recently Used (MRU) list so the app
                // retains persistent access permission to re-read the folder and its contents in future sessions.
                _filesService.AddToRecent(folder);
            }

            session = await TryParseQueueAsync(storageItems, cancellationToken: cancellationToken);
            if (session is null) return null;
        }

        session.NeighboringFilesQuery = neighboringFilesQuery;
        return session;
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async Task<QueueSession?> TryParseQueueAsync(
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

        var session = new QueueSession(next ?? queue[0], queue);
        if (storageItems.Count == 1 && storageItems[0] is StorageFolder rootFolder)
        {
            string folderTitle = !string.IsNullOrWhiteSpace(rootFolder.DisplayName)
                ? rootFolder.DisplayName
                : rootFolder.Name;
            session.Origin = QueueOrigin.ForMediaCollection(folderTitle, rootFolder.Path);
        }
        else if (storageItems.Count == 1 && storageItems[0] is StorageFile singleFile && singleFile.IsSupportedPlaylist())
        {
            string title = Path.GetFileNameWithoutExtension(singleFile.Name);
            session.Origin = QueueOrigin.ForMediaCollection(string.IsNullOrWhiteSpace(title) ? singleFile.Name : title, singleFile.Path);
        }
        else if (queue.Count > 1)
        {
            session.Origin = QueueOrigin.ForMediaCollection(queue[0].Name);
        }
        else
        {
            session.Origin = QueueOrigin.ForSingleMedia(queue[0]);
        }

        return session;
    }

    public async Task<QueueSession> ParseQueueAsync(MediaViewModel media, CancellationToken cancellationToken = default)
    {
        if (media.Source is IExpandableMediaSource expandable)
        {
            var items = await expandable.ExpandAsync(cancellationToken);
            if (items.Count > 0)
            {
                var vms = items.Select(_mediaFactory.Create).ToList();
                string title = Path.GetFileNameWithoutExtension(media.Name);
                return new QueueSession(0, vms)
                {
                    Origin = QueueOrigin.ForMediaCollection(
                        string.IsNullOrWhiteSpace(title) ? media.Name : title,
                        media.Location)
                };
            }
        }

        return new QueueSession(media)
        {
            Origin = QueueOrigin.ForSingleMedia(media)
        };
    }

    public async Task<QueueSession> ParseQueueAsync(StorageFile file, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(file);
        return await ParseQueueAsync(media, cancellationToken);
    }

    public async Task<QueueSession> ParseQueueAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(uri);
        return await ParseQueueAsync(media, cancellationToken);
    }

    public async Task<QueueSession> AddNeighboringFilesAsync(QueueSession session, CancellationToken cancellationToken = default)
    {
        if (session.NeighboringFilesQuery is not { } neighboringFilesQuery)
        {
            return session;
        }

        var neighboringFiles = await neighboringFilesQuery.GetFilesAsync();
        var result = await TryParseQueueAsync(neighboringFiles, null, expandPlaylists: false, cancellationToken);
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
                    // When expanding single-file playback into a full folder queue, register the parent folder
                    // with the Windows MRU list so persistent read permission is granted across future sessions.
                    _filesService.AddToRecent(parentFolder);
                }
            }
            catch
            {
                // Best-effort folder inspection
            }

            var currentItem = session.CurrentItem;
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

            var folderOrigin = QueueOrigin.ForMediaCollection(folderName, folderPath);

            if (currentItem is not null)
            {
                // Replace the matching item (by location) with the existing CurrentItem to preserve
                // VM identity. Without this, GetOrCreate creates a new VM for the same file that is a
                // different object reference. QueueSession uses IndexOf (reference equality) to find
                // CurrentItem in the new list; if it fails, CurrentIndex becomes -1, which causes
                // LoadFromPlaylist to set PlaybackItem to null and call VlcPlayer.Stop() on the UI
                // thread, freezing the app.
                int matchIndex = result.Items.FindIndex(vm =>
                    vm.Location.Equals(currentItem.Location, StringComparison.OrdinalIgnoreCase));
                if (matchIndex >= 0)
                {
                    result.Items[matchIndex] = currentItem;
                    return new QueueSession(currentItem, result.Items, session)
                    {
                        Origin = folderOrigin
                    };
                }

                // Current item not found in neighboring files (edge case).
                // Return the queue session unchanged to avoid losing the current position.
                return session;
            }

            return new QueueSession(result.Items, session)
            {
                Origin = folderOrigin
            };
        }

        return session;
    }

    public QueueSession Shuffle(QueueSession session, int? preserveIndex = null)
    {
        var shuffleBackup = new ShuffleBackup(new List<MediaViewModel>(session.Items));
        var shuffled = new QueueSession(session)
        {
            ShuffleMode = true,
            ShuffleBackup = shuffleBackup
        };

        var random = new Random();

        if (preserveIndex.HasValue && preserveIndex.Value >= 0 && preserveIndex.Value < shuffled.Items.Count)
        {
            var activeItem = shuffled.Items[preserveIndex.Value];
            shuffled.Items.RemoveAt(preserveIndex.Value);
            ShuffleList(shuffled.Items, random);
            shuffled.Items.Insert(0, activeItem);
            shuffled.CurrentIndex = 0;
        }
        else
        {
            ShuffleList(shuffled.Items, random);
        }

        return shuffled;
    }

    public QueueSession RestoreFromShuffle(QueueSession session)
    {
        Guard.IsNotNull(session.ShuffleBackup, nameof(session.ShuffleBackup));
        var shuffleBackup = session.ShuffleBackup;
        var backup = new List<MediaViewModel>(shuffleBackup.OriginalItems);

        foreach (var removal in shuffleBackup.Removals)
        {
            backup.Remove(removal);
        }

        var restored = session.CurrentItem != null
            ? new QueueSession(session.CurrentItem, backup, session)
            : new QueueSession(backup, session);
        restored.ShuffleMode = false;
        restored.ShuffleBackup = null;
        return restored;
    }

    public void UpdateShuffleBackup(QueueSession session, NotifyCollectionChangedEventArgs e)
    {
        if (session.Items.Count <= 1)
        {
            session.ShuffleBackup = null;
            return;
        }

        ShuffleBackup? backup = session.ShuffleBackup;
        if (!session.ShuffleMode || backup is null)
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
                    int backupIndex = backup.OriginalItems.IndexOf(oldItem);
                    if (backupIndex >= 0)
                    {
                        backup.OriginalItems[backupIndex] = newItem;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                foreach (object item in e.NewItems)
                {
                    if (item is MediaViewModel vm && !backup.Removals.Remove(vm))
                    {
                        session.ShuffleBackup = null;
                        break;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                session.ShuffleBackup = null;
                break;
        }
    }

    public IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int queueCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5)
    {
        if (currentIndex < 0 || queueCount == 0) return Array.Empty<int>();

        int startIndex = Math.Max(currentIndex - 2, 0);
        int endIndex = Math.Min(currentIndex + 2, queueCount - 1);
        var indices = new List<int>();

        for (int i = startIndex; i <= endIndex; i++)
        {
            indices.Add(i);
        }

        // Add wrap-around indices for list repeat mode
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && indices.Count < bufferSize)
        {
            if (startIndex == 0 && endIndex < queueCount - 1)
            {
                indices.Add(queueCount - 1);
            }

            if (startIndex > 0 && endIndex == queueCount - 1)
            {
                indices.Insert(0, 0);
            }
        }

        return indices.AsReadOnly();
    }

    private static void ShuffleList<T>(IList<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}

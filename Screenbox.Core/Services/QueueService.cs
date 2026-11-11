using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Screenbox.Core.Contexts;
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
    private const int MaxQueueSize = 10000;

    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IFilesService _filesService;
    private readonly GeneralSettingsContext? _generalSettings;

    public QueueService(MediaViewModelFactory mediaFactory, IFilesService filesService, GeneralSettingsContext? generalSettings = null)
    {
        _mediaFactory = mediaFactory;
        _filesService = filesService;
        _generalSettings = generalSettings;
    }

    #region Queue Creation & Parsing

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    public async Task<QueueSession?> TryCreateQueueAsync(
        IReadOnlyList<IStorageItem> storageItems,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default)
    {
        if (storageItems.Count == 0) return null;

        if (storageItems.Count == 1 && storageItems[0] is StorageFile file)
        {
            return await CreateSingleFileQueueAsync(file, neighboringFilesQuery, cancellationToken);
        }

        if (storageItems.Count == 1 && storageItems[0] is StorageFolder folder)
        {
            TrackInRecentIfEnabled(folder);
        }

        var session = await TryParseQueueAsync(storageItems, cancellationToken: cancellationToken);
        if (session is null) return null;

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

        for (int i = 0; i < storageItemQueue.Count && queue.Count < MaxQueueSize; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (storageItemQueue[i])
            {
                case StorageFile storageFile when storageFile.IsSupported():
                    bool isPlayNext = playNext is not null && storageFile.IsEqual(playNext);
                    var targetVm = await AppendStorageFileAsync(storageFile, queue, isPlayNext, expandPlaylists, cancellationToken);
                    next ??= targetVm;
                    break;

                case StorageFolder storageFolder:
                    var subItems = await storageFolder.GetItemsAsync(0, MaxQueueSize);
                    storageItemQueue.AddRange(subItems);
                    break;
            }
        }

        if (queue.Count == 0)
        {
            return null;
        }

        return new QueueSession(next ?? queue[0], queue)
        {
            Origin = ResolveStorageItemsOrigin(storageItems, queue)
        };
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

    private async Task<QueueSession> CreateSingleFileQueueAsync(
        StorageFile file,
        StorageFileQueryResult? neighboringFilesQuery,
        CancellationToken cancellationToken)
    {
        var session = await ParseQueueAsync(file, cancellationToken);
        if (session.Items.Count == 1 && session.Origin?.Kind == RecentItemKind.SingleMedia)
        {
            neighboringFilesQuery ??= await _filesService.GetNeighboringFilesQueryAsync(file);
        }
        else if (session.Origin?.Kind == RecentItemKind.MediaCollection)
        {
            TrackInRecentIfEnabled(file);
        }

        session.NeighboringFilesQuery = neighboringFilesQuery;
        return session;
    }

    private async Task<MediaViewModel?> AppendStorageFileAsync(
        StorageFile storageFile,
        List<MediaViewModel> queue,
        bool isPlayNext,
        bool expandPlaylists,
        CancellationToken cancellationToken)
    {
        var vm = _mediaFactory.GetOrCreate(storageFile);
        if (vm.Source is not IExpandableMediaSource expandable)
        {
            queue.Add(vm);
            return isPlayNext ? vm : null;
        }

        // Skip expandable items (e.g. .m3u in folder) when expandPlaylists is false and not explicitly selected.
        if (!expandPlaylists && !isPlayNext)
        {
            return null;
        }

        var expandedItems = await expandable.ExpandAsync(cancellationToken);
        if (expandedItems.Count == 0)
        {
            queue.Add(vm);
            return isPlayNext ? vm : null;
        }

        var childVms = expandedItems.Select(_mediaFactory.Create).ToList();
        queue.AddRange(childVms);
        return isPlayNext ? childVms[0] : null;
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private static QueueOrigin ResolveStorageItemsOrigin(
        IReadOnlyList<IStorageItem> storageItems,
        IReadOnlyList<MediaViewModel> queue)
    {
        if (storageItems.Count == 1 && storageItems[0] is StorageFolder rootFolder)
        {
            string folderTitle = !string.IsNullOrWhiteSpace(rootFolder.DisplayName)
                ? rootFolder.DisplayName
                : rootFolder.Name;
            return QueueOrigin.ForMediaCollection(folderTitle, rootFolder.Path);
        }

        if (storageItems.Count == 1 && storageItems[0] is StorageFile singleFile && singleFile.IsSupportedPlaylist())
        {
            string title = Path.GetFileNameWithoutExtension(singleFile.Name);
            return QueueOrigin.ForMediaCollection(
                string.IsNullOrWhiteSpace(title) ? singleFile.Name : title,
                singleFile.Path);
        }

        return queue.Count > 1
            ? QueueOrigin.ForMediaCollection(queue[0].Name)
            : QueueOrigin.ForSingleMedia(queue[0]);
    }

    #endregion

    #region Neighboring Files Expansion

    public async Task<QueueSession> AddNeighboringFilesAsync(QueueSession session, CancellationToken cancellationToken = default)
    {
        if (session.NeighboringFilesQuery is not { } neighboringFilesQuery)
        {
            return session;
        }

        var neighboringFiles = await neighboringFilesQuery.GetFilesAsync();
        var result = await TryParseQueueAsync(neighboringFiles, null, expandPlaylists: false, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (result is null || result.Items.Count <= 1)
        {
            return session;
        }

        var folderOrigin = ResolveNeighboringFolderOrigin(neighboringFilesQuery, session.CurrentItem, result.Items[0]);
        return MergeCurrentItemIntoNeighboringQueue(session, result.Items, folderOrigin);
    }

    private QueueOrigin ResolveNeighboringFolderOrigin(
        StorageFileQueryResult neighboringFilesQuery,
        MediaViewModel? currentItem,
        MediaViewModel fallbackItem)
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
                TrackInRecentIfEnabled(parentFolder);
            }
        }
        catch
        {
            // Best-effort folder inspection
        }

        if (string.IsNullOrWhiteSpace(folderName) && currentItem is not null)
        {
            folderPath ??= Path.GetDirectoryName(currentItem.Location);
            folderName = !string.IsNullOrWhiteSpace(folderPath)
                ? Path.GetFileName(folderPath)
                : currentItem.Name;
        }

        if (string.IsNullOrWhiteSpace(folderName))
        {
            folderName = fallbackItem.Name;
        }

        return QueueOrigin.ForMediaCollection(folderName, folderPath);
    }

    private static QueueSession MergeCurrentItemIntoNeighboringQueue(
        QueueSession session,
        List<MediaViewModel> neighboringItems,
        QueueOrigin folderOrigin)
    {
        if (session.CurrentItem is not { } currentItem)
        {
            return new QueueSession(neighboringItems, session)
            {
                Origin = folderOrigin
            };
        }

        // Replace the matching item (by location) with the existing CurrentItem to preserve
        // VM identity. Without this, GetOrCreate creates a new VM for the same file that is a
        // different object reference. QueueSession uses IndexOf (reference equality) to find
        // CurrentItem in the new list; if it fails, CurrentIndex becomes -1, which causes
        // LoadFromPlaylist to set PlaybackItem to null and call VlcPlayer.Stop() on the UI
        // thread, freezing the app.
        int matchIndex = neighboringItems.FindIndex(vm =>
            vm.Location.Equals(currentItem.Location, StringComparison.OrdinalIgnoreCase));
        if (matchIndex >= 0)
        {
            neighboringItems[matchIndex] = currentItem;
            return new QueueSession(currentItem, neighboringItems, session)
            {
                Origin = folderOrigin
            };
        }

        // Current item not found in neighboring files (edge case).
        // Return the queue session unchanged to avoid losing the current position.
        return session;
    }

    #endregion

    #region Shuffle & Buffer Management

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
                foreach (MediaViewModel vm in e.OldItems.OfType<MediaViewModel>())
                {
                    backup.Removals.Add(vm);
                }
                break;

            case NotifyCollectionChangedAction.Replace when e is
            {
                OldItems: { Count: { } oldCount } oldItems,
                NewItems: { Count: { } newCount } newItems
            } && oldCount == newCount && newCount > 0:
                ApplyShuffleReplace(backup, oldItems, newItems);
                break;

            case NotifyCollectionChangedAction.Add when e.NewItems is { } newItems:
                ApplyShuffleAdd(session, backup, newItems);
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

    private static void ApplyShuffleReplace(ShuffleBackup backup, IList oldItems, IList newItems)
    {
        for (int i = 0; i < oldItems.Count; i++)
        {
            if (oldItems[i] is not MediaViewModel oldItem || newItems[i] is not MediaViewModel newItem)
            {
                continue;
            }

            int backupIndex = backup.OriginalItems.IndexOf(oldItem);
            if (backupIndex >= 0)
            {
                backup.OriginalItems[backupIndex] = newItem;
            }
        }
    }

    private static void ApplyShuffleAdd(QueueSession session, ShuffleBackup backup, IList newItems)
    {
        foreach (MediaViewModel vm in newItems.OfType<MediaViewModel>())
        {
            if (!backup.Removals.Remove(vm))
            {
                session.ShuffleBackup = null;
                break;
            }
        }
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

    #endregion

    private void TrackInRecentIfEnabled(IStorageItem item)
    {
        // Register the storage item (e.g. playlist file or folder) with the Windows Most Recently Used (MRU)
        // list so the app retains persistent file-access permission to reload it across future sessions.
        if (_generalSettings?.ShowRecent != false)
        {
            _filesService.AddToRecent(item);
        }
    }
}

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

        string? containerToken = null;
        if (storageItems.Count == 1 && storageItems[0] is StorageFolder folder)
        {
            containerToken = TrackInRecentIfEnabled(folder);
        }

        var session = await TryParseQueueAsync(storageItems, cancellationToken: cancellationToken);
        if (session is null) return null;

        if (!string.IsNullOrEmpty(containerToken) && session.CurrentItem?.Origin is { Kind: RecentItemKind.MediaCollection } origin)
        {
            session.SetOrigin(origin with { Token = containerToken });
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

        return new QueueSession(next ?? queue[0], queue, origin: ResolveStorageItemsOrigin(storageItems, queue));
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
                string? token = (media.Source as IFileMediaSource)?.Token;
                var origin = QueueOrigin.ForMediaCollection(
                    string.IsNullOrWhiteSpace(title) ? media.Name : title,
                    media.Location,
                    token);
                return new QueueSession(0, vms, origin: origin);
            }
        }

        return new QueueSession(media);
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
        if (session.Items.Count == 1 && session.CurrentItem?.Origin is null)
        {
            neighboringFilesQuery ??= await _filesService.GetNeighboringFilesQueryAsync(file);
        }
        else if (session.CurrentItem?.Origin is { Kind: RecentItemKind.MediaCollection } origin)
        {
            string? fileToken = TrackInRecentIfEnabled(file);
            if (!string.IsNullOrEmpty(fileToken))
            {
                session.SetOrigin(origin with { Token = fileToken });
            }
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
    private static QueueOrigin? ResolveStorageItemsOrigin(
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

        // Loose multi-file drops/selections track whichever item plays individually.
        return queue.Count == 1 ? QueueOrigin.ForSingleMedia(queue[0]) : null;
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

        // Neighboring files auto-enqueued around a single opened file remain individually tracked
        // (Origin = null); only explicitly opened/dropped folders create a MediaCollection origin.
        return MergeCurrentItemIntoNeighboringQueue(session, result.Items);
    }

    private static QueueSession MergeCurrentItemIntoNeighboringQueue(
        QueueSession session,
        List<QueueItemViewModel> neighboringItems)
    {
        if (session.CurrentItem is not { } currentItem)
        {
            return new QueueSession(neighboringItems, session);
        }

        // Replace the matching item (by location) with the existing CurrentItem to preserve
        // queue item identity. Without this, GetOrCreate creates a new VM / QueueItemViewModel
        // that is a different object reference. QueueSession uses IndexOf (reference equality) to
        // find CurrentItem in the new list; if it fails, CurrentIndex becomes -1, which causes
        // LoadFromPlaylist to set PlaybackItem to null and call VlcPlayer.Stop() on the UI
        // thread, freezing the app.
        int matchIndex = neighboringItems.FindIndex(item =>
            item.Media.Location.Equals(currentItem.Media.Location, StringComparison.OrdinalIgnoreCase));
        if (matchIndex >= 0)
        {
            neighboringItems[matchIndex] = currentItem;
            return new QueueSession(currentItem, neighboringItems, session);
        }

        // Current item not found in neighboring files (edge case).
        // Return the queue session unchanged to avoid losing the current position.
        return session;
    }

    #endregion

    #region Shuffle & Buffer Management

    public QueueSession Shuffle(QueueSession session, int? preserveIndex = null)
    {
        var shuffleBackup = new ShuffleBackup(new List<QueueItemViewModel>(session.Items));
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
        var backup = new List<QueueItemViewModel>(shuffleBackup.OriginalItems);

        foreach (var removal in shuffleBackup.Removals)
        {
            backup.Remove(removal);
        }

        var restored = session.CurrentItem is not null
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
                foreach (QueueItemViewModel item in e.OldItems.OfType<QueueItemViewModel>())
                {
                    backup.Removals.Add(item);
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
            if (oldItems[i] is not QueueItemViewModel oldItem || newItems[i] is not QueueItemViewModel newItem)
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
        foreach (QueueItemViewModel item in newItems.OfType<QueueItemViewModel>())
        {
            if (!backup.Removals.Remove(item))
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

    private string? TrackInRecentIfEnabled(IStorageItem item)
    {
        // Register the storage item (e.g. playlist file or folder) with the Windows Most Recently Used (MRU)
        // list so the app retains persistent file-access permission to reload it across future sessions.
        if (_generalSettings?.ShowRecent != false)
        {
            return _filesService.AddToRecent(item);
        }

        return null;
    }
}

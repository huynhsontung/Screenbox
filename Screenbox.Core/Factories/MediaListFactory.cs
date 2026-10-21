using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Factories;

public sealed class MediaListFactory : IMediaListFactory
{
    private readonly MediaViewModelFactory _mediaFactory;

    public MediaListFactory(MediaViewModelFactory mediaFactory)
    {
        _mediaFactory = mediaFactory;
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFolder))]
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async Task<NextMediaList?> TryParseMediaListAsync(
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
                    bool isPlayNext = playNext != null && storageFile.IsEqual(playNext);

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

        return queue.Count > 0 ? new NextMediaList(next ?? queue[0], queue) : null;
    }

    public async Task<NextMediaList> ParseMediaListAsync(MediaViewModel media, CancellationToken cancellationToken = default)
    {
        if (media.Source is IExpandableMediaSource expandable)
        {
            var items = await expandable.ExpandAsync(cancellationToken);
            if (items.Count > 0)
            {
                var vms = items.Select(_mediaFactory.Create).ToList();
                return new NextMediaList(vms[0], vms);
            }
        }

        return new NextMediaList(media);
    }

    public async Task<NextMediaList> ParseMediaListAsync(StorageFile file, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(file);
        return await ParseMediaListAsync(media, cancellationToken);
    }

    public async Task<NextMediaList> ParseMediaListAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(uri);
        return await ParseMediaListAsync(media, cancellationToken);
    }

    public async Task<List<MediaViewModel>> ParseSubMediaRecursiveAsync(MediaViewModel source, CancellationToken cancellationToken = default)
    {
        if (source.Source is IExpandableMediaSource expandable)
        {
            var items = await expandable.ExpandAsync(cancellationToken);
            return items.Select(_mediaFactory.Create).ToList();
        }

        return new List<MediaViewModel>();
    }
}

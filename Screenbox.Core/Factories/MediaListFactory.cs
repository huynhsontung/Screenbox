using System.IO;
using System;
using System.Collections.Generic;
using System.IO;
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
    public async Task<Playlist?> TryParseMediaListAsync(
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

    public async Task<Playlist> ParseMediaListAsync(MediaViewModel media, CancellationToken cancellationToken = default)
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

    public async Task<Playlist> ParseMediaListAsync(StorageFile file, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(file);
        return await ParseMediaListAsync(media, cancellationToken);
    }

    public async Task<Playlist> ParseMediaListAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetOrCreate(uri);
        return await ParseMediaListAsync(media, cancellationToken);
    }
}

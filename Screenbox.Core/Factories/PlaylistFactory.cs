#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Factories;

public sealed class PlaylistFactory : IPlaylistFactory
{
    private readonly MediaViewModelFactory _mediaFactory;

    public PlaylistFactory(MediaViewModelFactory mediaFactory)
    {
        _mediaFactory = mediaFactory;
    }

    public async Task<Playlist> CreatePlaylistAsync(IReadOnlyList<IStorageItem> storageItems, StorageFile? playNext = null, Playlist? reference = null, CancellationToken cancellationToken = default)
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
                    var vm = _mediaFactory.GetSingleton(storageFile);
                    if (playNext != null && storageFile.IsEqual(playNext))
                    {
                        next = vm;
                    }

                    if (storageFile.IsSupportedPlaylist() && await ParseSubMediaRecursiveAsync(vm, cancellationToken) is { Count: > 0 } playlist)
                    {
                        queue.AddRange(playlist);
                    }
                    else
                    {
                        queue.Add(vm);
                    }
                    break;

                case StorageFolder storageFolder:
                    // Max number of items in a folder is 10k. Reevaluate if needed.
                    var subItems = await storageFolder.GetItemsAsync(0, 10000);
                    storageItemQueue.AddRange(subItems);
                    break;
            }
        }

        return queue.Count > 0 ? new Playlist(next ?? queue[0], queue, reference) : new Playlist(reference);
    }

    public async Task<Playlist> CreatePlaylistAsync(MediaViewModel media, CancellationToken cancellationToken = default)
    {
        // The ordering of the conditional terms below is important
        // Delay check Item as much as possible. Item is lazy init.
        if ((media.Source is StorageFile file && !file.IsSupportedPlaylist())
            || media.Source is Uri uri && !IsUriLocalPlaylistFile(uri)
            || media.Item.Value?.Media is { ParsedStatus: MediaParsedStatus.Done or MediaParsedStatus.Failed, SubItems.Count: 0 }
            || await ParseSubMediaRecursiveAsync(media, cancellationToken) is not { Count: > 0 } playlist)
        {
            return new Playlist(media, new List<MediaViewModel> { media });
        }

        return new Playlist(playlist[0], playlist);
    }

    public async Task<Playlist> CreatePlaylistAsync(StorageFile file, Playlist? reference = null, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetSingleton(file);
        if (file.IsSupportedPlaylist() && await ParseSubMediaRecursiveAsync(media, cancellationToken) is { Count: > 0 } items)
        {
            media = items[0];
            return new Playlist(media, items, reference);
        }

        return new Playlist(media, new List<MediaViewModel> { media }, reference);
    }

    public async Task<Playlist> CreatePlaylistAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var media = _mediaFactory.GetTransient(uri);
        if (await ParseSubMediaRecursiveAsync(media, cancellationToken) is { Count: > 0 } playlist)
        {
            media = playlist[0];
            return new Playlist(media, playlist);
        }

        return new Playlist(media, new List<MediaViewModel> { media });
    }

    public async Task<List<MediaViewModel>> ParseSubMediaRecursiveAsync(MediaViewModel source, CancellationToken cancellationToken = default)
    {
        var items = await ParseSubMediaAsync(source, cancellationToken);
        if (items.Count > 0)
        {
            var nextItem = items[0];
            while (items.Count == 1 && await ParseSubMediaAsync(nextItem, cancellationToken) is { Count: > 0 } nextSubItems)
            {
                nextItem = nextSubItems[0];
                items = nextSubItems;
            }
        }

        return items;
    }

    private async Task<List<MediaViewModel>> ParseSubMediaAsync(MediaViewModel source, CancellationToken cancellationToken = default)
    {
        if (source.Item.Value == null) return new List<MediaViewModel>();

        try
        {
            var media = source.Item.Value.Media;
            if (!media.IsParsed || media.ParsedStatus is MediaParsedStatus.Skipped)
            {
                await media.ParseAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var subItems = media.SubItems.Select(item => _mediaFactory.GetTransient(item));
            return subItems.ToList();
        }
        catch (OperationCanceledException)
        {
            return new List<MediaViewModel>();
        }
    }

    private static bool IsUriLocalPlaylistFile(Uri uri)
    {
        if (!uri.IsAbsoluteUri || !uri.IsLoopback || !uri.IsFile) return false;
        var extension = Path.GetExtension(uri.LocalPath);
        return FilesHelpers.SupportedPlaylistFormats.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}

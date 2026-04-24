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

public sealed class MediaListFactory : IMediaListFactory
{
    private readonly MediaViewModelFactory _mediaFactory;

    public MediaListFactory(MediaViewModelFactory mediaFactory)
    {
        _mediaFactory = mediaFactory;
    }

    public async Task<NextMediaList?> TryParseMediaListAsync(IReadOnlyList<IStorageItem> storageItems, StorageFile? playNext = null, CancellationToken cancellationToken = default)
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
                    if (IsM3uPlaylist(storageFile.FileType))
                    {
                        // Parse M3U/M3U8 playlists directly without creating a LibVLC Media object.
                        var m3uItems = await ParseM3uAsync(storageFile, cancellationToken);
                        if (m3uItems.Count > 0)
                        {
                            if (playNext != null && storageFile.IsEqual(playNext))
                                next = m3uItems[0];
                            queue.AddRange(m3uItems);
                        }
                        else
                        {
                            // Fallback: add the playlist file itself if parsing yielded no items.
                            var fallback = _mediaFactory.GetSingleton(storageFile);
                            if (playNext != null && storageFile.IsEqual(playNext))
                                next = fallback;
                            queue.Add(fallback);
                        }
                    }
                    else
                    {
                        var vm = _mediaFactory.GetSingleton(storageFile);
                        if (playNext != null && storageFile.IsEqual(playNext))
                            next = vm;

                        if (storageFile.IsSupportedPlaylist() && await ParseSubMediaRecursiveAsync(vm, cancellationToken) is { Count: > 0 } playlist)
                            queue.AddRange(playlist);
                        else
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

        return queue.Count > 0 ? new NextMediaList(next ?? queue[0], queue) : null;
    }

    public async Task<NextMediaList> ParseMediaListAsync(MediaViewModel media, CancellationToken cancellationToken = default)
    {
        // Handle M3U/M3U8 sources directly without going through LibVLC media parsing.
        var m3uFile = await TryGetM3uStorageFileAsync(media.Source);
        if (m3uFile is not null)
        {
            var m3uItems = await ParseM3uAsync(m3uFile, cancellationToken);
            if (m3uItems.Count > 0)
                return new NextMediaList(m3uItems[0], m3uItems);
            return new NextMediaList(media);
        }

        // The ordering of the conditional terms below is important
        // Delay check Item as much as possible. Item is lazy init.
        if ((media.Source is StorageFile file && !file.IsSupportedPlaylist())
            || media.Source is Uri uri && !IsUriLocalPlaylistFile(uri)
            || media.Item.Value?.Media is { ParsedStatus: MediaParsedStatus.Done or MediaParsedStatus.Failed, SubItems.Count: 0 }
            || await ParseSubMediaRecursiveAsync(media, cancellationToken) is not { Count: > 0 } playlist)
        {
            return new NextMediaList(media);
        }

        return new NextMediaList(playlist[0], playlist);
    }

    public async Task<NextMediaList> ParseMediaListAsync(StorageFile file, CancellationToken cancellationToken = default)
    {
        if (IsM3uPlaylist(file.FileType))
        {
            // Parse M3U/M3U8 playlists directly without creating a LibVLC Media object.
            var m3uItems = await ParseM3uAsync(file, cancellationToken);
            if (m3uItems.Count > 0)
                return new NextMediaList(m3uItems[0], m3uItems);

            // Fallback: treat the playlist file itself as the media item.
            return new NextMediaList(_mediaFactory.GetSingleton(file));
        }

        var media = _mediaFactory.GetSingleton(file);
        if (file.IsSupportedPlaylist() && await ParseSubMediaRecursiveAsync(media, cancellationToken) is { Count: > 0 } items)
        {
            media = items[0];
            return new NextMediaList(media, items);
        }

        return new NextMediaList(media);
    }

    public async Task<NextMediaList> ParseMediaListAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (IsUriLocalM3uFile(uri))
        {
            // Convert local M3U/M3U8 URIs to StorageFile and parse directly without LibVLC.
            var file = await TryGetStorageFileFromPathAsync(uri.LocalPath);
            if (file is not null)
            {
                var m3uItems = await ParseM3uAsync(file, cancellationToken);
                if (m3uItems.Count > 0)
                    return new NextMediaList(m3uItems[0], m3uItems);
            }

            // Fallback: treat the URI as a regular media item.
            return new NextMediaList(_mediaFactory.GetSingleton(uri));
        }

        var media = _mediaFactory.GetTransient(uri);
        if (await ParseSubMediaRecursiveAsync(media, cancellationToken) is { Count: > 0 } playlist)
        {
            media = playlist[0];
            return new NextMediaList(media, playlist);
        }

        return new NextMediaList(media);
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

    /// <summary>
    /// Parses an M3U or M3U8 playlist file directly, without invoking LibVLC media parsing.
    /// Each non-comment, non-empty line is resolved as either an absolute URI or a path
    /// (absolute or relative to the playlist file's directory) and wrapped in a
    /// <see cref="MediaViewModel"/>.
    /// </summary>
    /// <param name="playlistFile">The M3U or M3U8 playlist file to parse.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A list of <see cref="MediaViewModel"/> instances for each entry in the playlist.</returns>
    private async Task<List<MediaViewModel>> ParseM3uAsync(StorageFile playlistFile, CancellationToken cancellationToken = default)
    {
        string content;
        try
        {
            using var stream = await playlistFile.OpenStreamForReadAsync();
            // Detect encoding from BOM; M3U8 is UTF-8, M3U may be system-default.
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            content = await reader.ReadToEndAsync();
        }
        catch (Exception)
        {
            return new List<MediaViewModel>();
        }

        var result = new List<MediaViewModel>();
        var baseDirectory = Path.GetDirectoryName(playlistFile.Path) ?? string.Empty;

        foreach (var rawLine in content.Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = rawLine.Trim();
            // Skip empty lines and M3U directive/comment lines (those starting with '#').
            if (string.IsNullOrEmpty(line) || line[0] == '#')
                continue;

            // Try to interpret the entry as an absolute URI first (e.g. http://, file://).
            if (Uri.TryCreate(line, UriKind.Absolute, out Uri? uri))
            {
                if (uri.IsFile && uri.IsLoopback)
                {
                    // Local file URI — prefer StorageFile for richer metadata support.
                    var localFile = await TryGetStorageFileFromPathAsync(uri.LocalPath);
                    result.Add(localFile is not null
                        ? _mediaFactory.GetSingleton(localFile)
                        : _mediaFactory.GetSingleton(uri));
                }
                else
                {
                    result.Add(_mediaFactory.GetTransient(uri));
                }

                continue;
            }

            // Treat the entry as a path (absolute or relative to the playlist directory).
            string resolvedPath;
            try
            {
                resolvedPath = Path.IsPathRooted(line)
                    ? Path.GetFullPath(line)
                    : Path.GetFullPath(Path.Combine(baseDirectory, line));
            }
            catch (Exception)
            {
                // Invalid path — skip this entry.
                continue;
            }

            var resolvedFile = await TryGetStorageFileFromPathAsync(resolvedPath);
            if (resolvedFile is not null)
            {
                result.Add(_mediaFactory.GetSingleton(resolvedFile));
            }
            else if (Uri.TryCreate(resolvedPath, UriKind.Absolute, out Uri? fileUri))
            {
                // Fall back to URI-based access when the file is not directly accessible.
                result.Add(_mediaFactory.GetSingleton(fileUri));
            }
        }

        return result;
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

    /// <summary>
    /// Returns the <see cref="StorageFile"/> for an M3U/M3U8 source object, if applicable.
    /// Handles both <see cref="StorageFile"/> sources and local <see cref="Uri"/> sources.
    /// Returns <see langword="null"/> when the source is not an M3U/M3U8 file.
    /// </summary>
    private static async Task<StorageFile?> TryGetM3uStorageFileAsync(object source)
    {
        return source switch
        {
            StorageFile file when IsM3uPlaylist(file.FileType) => file,
            Uri uri when IsUriLocalM3uFile(uri) => await TryGetStorageFileFromPathAsync(uri.LocalPath),
            _ => null
        };
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="StorageFile"/> from a file-system path.
    /// Returns <see langword="null"/> when the file is inaccessible or does not exist.
    /// </summary>
    private static async Task<StorageFile?> TryGetStorageFileFromPathAsync(string path)
    {
        try
        {
            return await StorageFile.GetFileFromPathAsync(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="extension"/> is
    /// <c>.m3u</c> or <c>.m3u8</c> (case-insensitive).
    /// </summary>
    private static bool IsM3uPlaylist(string extension)
        => extension.Equals(".m3u", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".m3u8", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="uri"/> refers to a local
    /// M3U or M3U8 file.
    /// </summary>
    private static bool IsUriLocalM3uFile(Uri uri)
    {
        if (!uri.IsAbsoluteUri || !uri.IsLoopback || !uri.IsFile) return false;
        return IsM3uPlaylist(Path.GetExtension(uri.LocalPath));
    }

    private static bool IsUriLocalPlaylistFile(Uri uri)
    {
        if (!uri.IsAbsoluteUri || !uri.IsLoopback || !uri.IsFile) return false;
        var extension = Path.GetExtension(uri.LocalPath);
        return FilesHelpers.SupportedPlaylistFormats.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}

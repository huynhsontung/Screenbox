#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

public sealed class PlaylistService
{
    private const string PlaylistsFolderName = "Playlists";
    private const string ThumbnailsFolderName = "Thumbnails";
    private readonly FilesService _filesService;
    private readonly IMediaListFactory _mediaListFactory;

    public PlaylistService(IMediaListFactory mediaListFactory, FilesService filesService)
    {
        _mediaListFactory = mediaListFactory;
        _filesService = filesService;
    }

    public async Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery, CancellationToken cancellationToken = default)
    {
        var neighboringFiles = await neighboringFilesQuery.GetFilesAsync();
        var result = await _mediaListFactory.TryParseMediaListAsync(neighboringFiles, null, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (result?.Items.Count > 0)
            return playlist.CurrentItem != null
                ? new Playlist(playlist.CurrentItem, result.Items, playlist)
                : new Playlist(result.Items, playlist);

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

    public async Task SavePlaylistAsync(PersistentPlaylist playlist)
    {
        StorageFolder playlistsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(PlaylistsFolderName, CreationCollisionOption.OpenIfExists);
        string fileName = playlist.Id + ".json";
        await _filesService.SaveToDiskAsync(playlistsFolder, fileName, playlist);
    }

    public async Task<PersistentPlaylist?> LoadPlaylistAsync(string id)
    {
        StorageFolder playlistsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(PlaylistsFolderName, CreationCollisionOption.OpenIfExists);
        string fileName = id + ".json";
        try
        {
            return await _filesService.LoadFromDiskAsync<PersistentPlaylist>(playlistsFolder, fileName);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<PersistentPlaylist>> ListPlaylistsAsync()
    {
        StorageFolder playlistsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(PlaylistsFolderName, CreationCollisionOption.OpenIfExists);
        var files = await playlistsFolder.GetFilesAsync();
        var playlists = new List<PersistentPlaylist>();
        foreach (var file in files)
        {
            try
            {
                var playlist = await _filesService.LoadFromDiskAsync<PersistentPlaylist>(file);
                if (playlist != null)
                    playlists.Add(playlist);
            }
            catch { }
        }
        return playlists;
    }

    public async Task DeletePlaylistAsync(string id)
    {
        StorageFolder playlistsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(PlaylistsFolderName, CreationCollisionOption.OpenIfExists);
        string fileName = id + ".json";
        try
        {
            StorageFile file = await playlistsFolder.GetFileAsync(fileName);
            await file.DeleteAsync();
        }
        catch { }
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
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input.ToLowerInvariant());
        byte[] hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

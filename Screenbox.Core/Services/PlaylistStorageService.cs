#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Screenbox.Core.ViewModels;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public sealed class PlaylistService
{
    private const string PlaylistsFolderName = "Playlists";
    private const string ThumbnailsFolderName = "Thumbnails";
    private readonly FilesService _filesService;

    public PlaylistService(FilesService filesService)
    {
        _filesService = filesService;
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

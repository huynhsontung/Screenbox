using System;
using System.IO;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Screenbox.Core.MediaSources;

public class FileMediaSource : IFileMediaSource
{
    public string Location => Path;

    public string Path { get; }

    public string FileType { get; private set; }

    public string Name { get; private set; }

    public MediaPlaybackType MediaType { get; }

    public StorageFile? StorageFile { get; private set; }

    public string? Token { get; set; }

    public FileMediaSource(StorageFile file)
    {
        StorageFile = file;
        Path = file.Path;
        FileType = file.FileType;
        Name = file.Name;
        MediaType = FilesHelpers.GetMediaTypeForFile(file);
    }

    public FileMediaSource(string path, string? token = null)
    {
        Path = path;
        FileType = System.IO.Path.GetExtension(path);
        Name = System.IO.Path.GetFileName(path);
        MediaType = FilesHelpers.GetMediaTypeForExtension(FileType);
        Token = token;
    }

    public async Task<StorageFile?> GetStorageFileAsync()
    {
        if (StorageFile != null)
        {
            return StorageFile;
        }

        if (!string.IsNullOrEmpty(Token))
        {
            try
            {
                if (StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(Token))
                {
                    StorageFile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(
                        Token,
                        AccessCacheOptions.SuppressAccessTimeUpdate);
                }
                else if (StorageApplicationPermissions.FutureAccessList.ContainsItem(Token))
                {
                    StorageFile = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(
                        Token,
                        AccessCacheOptions.SuppressAccessTimeUpdate);
                }
            }
            catch
            {
                // Access cache lookup failed; fall back to path
            }
        }

        if (StorageFile == null && !string.IsNullOrEmpty(Path))
        {
            StorageFile = await FilesHelpers.TryGetFileFromPathAsync(Path);
        }

        return StorageFile;
    }

    public void UpdateStorageFile(StorageFile file)
    {
        StorageFile = file;
        Name = file.Name;
        FileType = file.FileType;
    }

    public override string ToString() => Location;
}

using System;
using System.IO;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Windows.Storage;

namespace Screenbox.Core.MediaSources;

public class FileMediaSource : IFileMediaSource
{
    public string Location => Path;

    public string Path { get; }

    public string FileType { get; private set; }

    public string Name { get; private set; }

    public MediaPlaybackType MediaType { get; }

    public StorageFile? StorageFile { get; private set; }

    public FileMediaSource(StorageFile file)
    {
        StorageFile = file;
        Path = file.Path;
        FileType = file.FileType;
        Name = file.Name;
        MediaType = FilesHelpers.GetMediaTypeForFile(file);
    }

    public FileMediaSource(string path)
    {
        Path = path;
        FileType = System.IO.Path.GetExtension(path);
        Name = System.IO.Path.GetFileName(path);
        MediaType = FilesHelpers.GetMediaTypeForExtension(FileType);
    }

    public async Task<StorageFile?> GetStorageFileAsync()
    {
        if (StorageFile != null)
        {
            return StorageFile;
        }

        if (!string.IsNullOrEmpty(Path))
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

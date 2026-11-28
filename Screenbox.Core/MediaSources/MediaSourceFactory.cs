using System;
using System.IO;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.MediaSources;

public class MediaSourceFactory : IMediaSourceFactory
{
    private readonly IFilesService _filesService;

    public MediaSourceFactory(IFilesService filesService)
    {
        ArgumentNullException.ThrowIfNull(filesService);
        _filesService = filesService;
    }

    public IMediaSource Create(StorageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (IsM3u(file.FileType))
        {
            return new M3uPlaylistMediaSource(file, this, _filesService);
        }

        return new FileMediaSource(file);
    }

    public IMediaSource Create(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (uri is { IsAbsoluteUri: true, IsFile: true })
        {
            return Create(uri.LocalPath);
        }

        return new UriMediaSource(uri);
    }

    public IMediaSource Create(string pathOrUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathOrUri);

        if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out Uri? uri) && !uri.IsFile)
        {
            return new UriMediaSource(uri);
        }

        string path = uri is { IsFile: true } ? uri.LocalPath : pathOrUri;
        string extension = Path.GetExtension(path);
        if (IsM3u(extension))
        {
            return new M3uPlaylistMediaSource(path, this, _filesService);
        }

        return new FileMediaSource(path);
    }

    private static bool IsM3u(string extension) =>
        string.Equals(extension, ".m3u", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(extension, ".m3u8", StringComparison.OrdinalIgnoreCase);
}

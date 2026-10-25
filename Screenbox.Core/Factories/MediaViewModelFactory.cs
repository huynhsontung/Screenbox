using System;
using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.Contexts;
using Screenbox.Core.MediaSources;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Factories;

public sealed class MediaViewModelFactory
{
    private readonly IMediaSourceFactory _sourceFactory;
    private readonly LibraryContext _libraryContext;

    public MediaViewModelFactory(IMediaSourceFactory sourceFactory, LibraryContext libraryContext)
    {
        _sourceFactory = sourceFactory;
        _libraryContext = libraryContext;
    }

    /// <summary>
    /// Always creates a new <see cref="MediaViewModel"/> without any lookup.
    /// </summary>
    public MediaViewModel Create(IMediaSource source)
    {
        return new MediaViewModel(source);
    }

    /// <summary>
    /// Creates a new <see cref="MediaViewModel"/> populated from an <see cref="ExpandedMediaItem"/>.
    /// </summary>
    public MediaViewModel Create(ExpandedMediaItem item)
    {
        var vm = Create(item.Source);
        if (!string.IsNullOrEmpty(item.Title))
        {
            vm.Name = item.Title;
        }

        if (item.Duration.HasValue && item.Duration.Value > TimeSpan.Zero)
        {
            vm.MediaInfo.MusicProperties.Duration = item.Duration.Value;
            vm.MediaInfo.VideoProperties.Duration = item.Duration.Value;
        }

        return vm;
    }

    /// <summary>
    /// Always creates a new <see cref="MediaViewModel"/> without any lookup.
    /// Use this when building a library snapshot.
    /// </summary>
    public MediaViewModel Create(StorageFile file)
    {
        return new MediaViewModel(_sourceFactory.Create(file));
    }

    /// <summary>
    /// Always creates a new <see cref="MediaViewModel"/> without any lookup.
    /// </summary>
    public MediaViewModel Create(Uri uri)
    {
        return new MediaViewModel(_sourceFactory.Create(uri));
    }

    /// <summary>
    /// Always creates a new <see cref="MediaViewModel"/> without any lookup.
    /// </summary>
    public MediaViewModel Create(string pathOrUri)
    {
        return new MediaViewModel(_sourceFactory.Create(pathOrUri));
    }

    /// <summary>
    /// Returns an existing <see cref="MediaViewModel"/> from the current library state if found,
    /// otherwise creates a new one. Callers in the library service should use <see cref="Create"/> instead.
    /// </summary>
    public MediaViewModel GetOrCreate(StorageFile file)
    {
        TryGetOrCreate(file, out MediaViewModel mediaViewModel);
        return mediaViewModel;
    }

    /// <summary>
    /// Returns an existing <see cref="MediaViewModel"/> from the current library state if found,
    /// otherwise creates a new one.
    /// </summary>
    public MediaViewModel GetOrCreate(Uri uri)
    {
        TryGetOrCreate(uri, out MediaViewModel mediaViewModel);
        return mediaViewModel;
    }

    /// <summary>
    /// Returns an existing <see cref="MediaViewModel"/> from the current library state if found,
    /// otherwise creates a new one.
    /// </summary>
    public MediaViewModel GetOrCreate(string pathOrUri)
    {
        var existing = FindByLocation(_libraryContext.Music.Songs, pathOrUri)
                       ?? FindByLocation(_libraryContext.Videos.Videos, pathOrUri);
        return existing ?? Create(pathOrUri);
    }

    public bool TryGetOrCreate(StorageFile file, out MediaViewModel mediaViewModel)
    {
        string location = file.Path;
        var existing = FindByLocation(_libraryContext.Music.Songs, location)
                       ?? FindByLocation(_libraryContext.Videos.Videos, location);
        if (existing is null)
        {
            mediaViewModel = Create(file);
            return false;
        }

        if (existing.Source is IFileMediaSource fileSource && fileSource.StorageFile is null)
            fileSource.UpdateStorageFile(file);

        mediaViewModel = existing;
        return true;
    }

    public bool TryGetOrCreate(Uri uri, out MediaViewModel mediaViewModel)
    {
        string location = uri.IsFile ? uri.LocalPath : uri.OriginalString;
        var existing = FindByLocation(_libraryContext.Music.Songs, location)
                       ?? FindByLocation(_libraryContext.Videos.Videos, location);
        if (existing is null)
        {
            mediaViewModel = Create(uri);
            return false;
        }

        mediaViewModel = existing;
        return true;
    }

    private static MediaViewModel? FindByLocation(IReadOnlyList<MediaViewModel> mediaList, string location)
    {
        return mediaList.FirstOrDefault(vm =>
            vm.Location.Equals(location, StringComparison.OrdinalIgnoreCase));
    }
}

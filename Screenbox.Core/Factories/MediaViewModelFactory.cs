#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using LibVLCSharp.Shared;
using Screenbox.Core.Contexts;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Storage;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Factories;

public sealed class MediaViewModelFactory
{
    private readonly IPlayerService _playerService;
    private readonly PlayerContext _playerContext;
    private readonly LibraryContext _libraryContext;

    public MediaViewModelFactory(IPlayerService playerService, PlayerContext playerContext, LibraryContext libraryContext)
    {
        _playerService = playerService;
        _playerContext = playerContext;
        _libraryContext = libraryContext;
    }

    /// <summary>
    /// Always creates a new <see cref="MediaViewModel"/> without any lookup.
    /// Use this when building a library snapshot.
    /// </summary>
    public MediaViewModel Create(StorageFile file)
    {
        return new MediaViewModel(_playerContext, _playerService, file);
    }

    /// <summary>
    /// Always creates a new <see cref="MediaViewModel"/> without any lookup.
    /// </summary>
    public MediaViewModel Create(Uri uri)
    {
        return new MediaViewModel(_playerContext, _playerService, uri);
    }

    /// <summary>
    /// Always creates a new <see cref="MediaViewModel"/> without any lookup.
    /// </summary>
    public MediaViewModel Create(Media media)
    {
        if (!Uri.TryCreate(media.Mrl, UriKind.Absolute, out Uri uri))
            return new MediaViewModel(_playerContext, _playerService, media);

        // Prefer URI source for easier clean up
        MediaViewModel vm = new(_playerContext, _playerService, uri)
        {
            Item = new Lazy<PlaybackItem?>(new PlaybackItem(media, media))
        };

        if (media.Meta(MetadataType.Title) is { } name && !string.IsNullOrEmpty(name))
            vm.Name = name;

        return vm;
    }

    /// <summary>
    /// Returns an existing <see cref="MediaViewModel"/> from the current library state if found,
    /// otherwise creates a new one. Callers in the library service should use <see cref="Create"/> instead.
    /// </summary>
    public MediaViewModel GetOrCreate(StorageFile file)
    {
        string location = file.Path;
        var existing = FindByLocation(_libraryContext.Music.Songs, location)
                       ?? FindByLocation(_libraryContext.Videos.Videos, location);
        if (existing != null)
        {
            if (existing.Source is not IStorageFile)
                existing.UpdateSource(file);
            return existing;
        }

        return new MediaViewModel(_playerContext, _playerService, file);
    }

    /// <summary>
    /// Returns an existing <see cref="MediaViewModel"/> from the current library state if found,
    /// otherwise creates a new one.
    /// </summary>
    public MediaViewModel GetOrCreate(Uri uri)
    {
        string location = uri.OriginalString;
        var existing = FindByLocation(_libraryContext.Music.Songs, location)
                       ?? FindByLocation(_libraryContext.Videos.Videos, location);
        return existing ?? new MediaViewModel(_playerContext, _playerService, uri);
    }

    public bool TryGetOrCreate(StorageFile file, out MediaViewModel? mediaViewModel)
    {
        string location = file.Path;
        mediaViewModel = FindByLocation(_libraryContext.Music.Songs, location)
                         ?? FindByLocation(_libraryContext.Videos.Videos, location);
        if (mediaViewModel == null)
        {
            mediaViewModel = new MediaViewModel(_playerContext, _playerService, file);
            return false;
        }

        return true;
    }

    public bool TryGetOrCreate(Uri uri, out MediaViewModel? mediaViewModel)
    {
        string location = uri.OriginalString;
        mediaViewModel = FindByLocation(_libraryContext.Music.Songs, location)
                         ?? FindByLocation(_libraryContext.Videos.Videos, location);
        if (mediaViewModel == null)
        {
            mediaViewModel = new MediaViewModel(_playerContext, _playerService, uri);
            return false;
        }

        return true;
    }

    private static MediaViewModel? FindByLocation(IReadOnlyList<MediaViewModel> mediaList, string location)
    {
        return mediaList.FirstOrDefault(vm =>
            vm.Location.Equals(location, StringComparison.OrdinalIgnoreCase));
    }
}


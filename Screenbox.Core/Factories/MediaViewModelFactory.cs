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
    private readonly Dictionary<string, WeakReference<MediaViewModel>> _references = new();
    private int _referencesCleanUpThreshold = 1000;

    public MediaViewModelFactory(IPlayerService playerService, PlayerContext playerContext)
    {
        _playerService = playerService;
        _playerContext = playerContext;
    }

    public MediaViewModel GetTransient(StorageFile file)
    {
        return new MediaViewModel(_playerContext, _playerService, file);
    }

    public MediaViewModel GetTransient(Uri uri)
    {
        return new MediaViewModel(_playerContext, _playerService, uri);
    }

    public MediaViewModel GetTransient(Media media)
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

    public MediaViewModel GetSingleton(StorageFile file)
    {
        string id = file.Path;
        if (_references.TryGetValue(id, out WeakReference<MediaViewModel> reference) &&
            reference.TryGetTarget(out MediaViewModel instance))
        {
            // Prefer storage file source
            if (instance.Source is not IStorageFile)
            {
                instance.UpdateSource(file);
            }

            return instance;
        }


        // No existing reference, create new instance
        instance = new MediaViewModel(_playerContext, _playerService, file);
        if (!string.IsNullOrEmpty(id))
        {
            _references[id] = new WeakReference<MediaViewModel>(instance);
            CleanUpStaleReferences();
        }

        return instance;
    }

    public MediaViewModel GetSingleton(Uri uri)
    {
        string id = uri.OriginalString;
        if (_references.TryGetValue(id, out WeakReference<MediaViewModel> reference) &&
            reference.TryGetTarget(out MediaViewModel instance)) return instance;

        // No existing reference, create new instance
        instance = new MediaViewModel(_playerContext, _playerService, uri);
        if (!string.IsNullOrEmpty(id))
        {
            _references[id] = new WeakReference<MediaViewModel>(instance);
            CleanUpStaleReferences();
        }

        return instance;
    }

    private void CleanUpStaleReferences()
    {
        if (_references.Count < _referencesCleanUpThreshold) return;
        string[] keysToRemove = _references
            .Where(pair => !pair.Value.TryGetTarget(out MediaViewModel _))
            .Select(pair => pair.Key).ToArray();
        foreach (string key in keysToRemove)
        {
            _references.Remove(key);
        }

        _referencesCleanUpThreshold = Math.Max(_references.Count * 2, _referencesCleanUpThreshold);
    }
}

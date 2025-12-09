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
    private readonly LibVlcService _libVlcService;
    private readonly MediaViewModelFactoryContext State;

    public MediaViewModelFactory(LibVlcService libVlcService, MediaViewModelFactoryContext state)
    {
        _libVlcService = libVlcService;
        State = state;
    }

    public MediaViewModel GetTransient(StorageFile file)
    {
        return new MediaViewModel(_libVlcService, file);
    }

    public MediaViewModel GetTransient(Uri uri)
    {
        return new MediaViewModel(_libVlcService, uri);
    }

    public MediaViewModel GetTransient(Media media)
    {
        if (!Uri.TryCreate(media.Mrl, UriKind.Absolute, out Uri uri))
            return new MediaViewModel(_libVlcService, media);

        // Prefer URI source for easier clean up
        MediaViewModel vm = new(_libVlcService, uri)
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
        if (State.References.TryGetValue(id, out WeakReference<MediaViewModel> reference) &&
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
        instance = new MediaViewModel(_libVlcService, file);
        if (!string.IsNullOrEmpty(id))
        {
            State.References[id] = new WeakReference<MediaViewModel>(instance);
            CleanUpStaleReferences();
        }

        return instance;
    }

    public MediaViewModel GetSingleton(Uri uri)
    {
        string id = uri.OriginalString;
        if (State.References.TryGetValue(id, out WeakReference<MediaViewModel> reference) &&
            reference.TryGetTarget(out MediaViewModel instance)) return instance;

        // No existing reference, create new instance
        instance = new MediaViewModel(_libVlcService, uri);
        if (!string.IsNullOrEmpty(id))
        {
            State.References[id] = new WeakReference<MediaViewModel>(instance);
            CleanUpStaleReferences();
        }

        return instance;
    }

    private void CleanUpStaleReferences()
    {
        if (State.References.Count < State.ReferencesCleanUpThreshold) return;
        string[] keysToRemove = State.References
            .Where(pair => !pair.Value.TryGetTarget(out MediaViewModel _))
            .Select(pair => pair.Key).ToArray();
        foreach (string key in keysToRemove)
        {
            State.References.Remove(key);
        }

        State.ReferencesCleanUpThreshold = Math.Max(State.References.Count * 2, State.ReferencesCleanUpThreshold);
    }
}

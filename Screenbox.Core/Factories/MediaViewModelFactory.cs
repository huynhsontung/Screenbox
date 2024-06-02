using LibVLCSharp.Shared;
using Screenbox.Core.Playback;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Factories
{
    public sealed class MediaViewModelFactory
    {
        private readonly Dictionary<string, WeakReference<MediaViewModel>> _references = new();
        private int _referencesCleanUpThreshold = 1000;

        public MediaViewModel GetTransient(StorageFile file)
        {
            return new MediaViewModel(file);
        }

        public MediaViewModel GetTransient(Uri uri)
        {
            return new MediaViewModel(uri);
        }

        public MediaViewModel GetTransient(Media media)
        {
            if (!Uri.TryCreate(media.Mrl, UriKind.Absolute, out Uri uri))
                return new MediaViewModel(media);

            // Prefer URI source for easier clean up
            MediaViewModel vm = new(uri)
            {
                Item = new VlcPlaybackItem(media, media)
            };

            if (media.Meta(MetadataType.Title) is { } name)
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
            instance = new MediaViewModel(file);
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
            instance = new MediaViewModel(uri);
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
}

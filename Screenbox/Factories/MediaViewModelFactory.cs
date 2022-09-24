using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Screenbox.Services;
using Screenbox.ViewModels;

namespace Screenbox.Factories
{
    internal class MediaViewModelFactory
    {
        private readonly IFilesService _filesService;
        private readonly IMediaService _mediaService;
        private readonly Dictionary<string, WeakReference<MediaViewModel>> _references = new();
        private int _referencesCleanUpThreshold = 500;

        public MediaViewModelFactory(IFilesService filesService, IMediaService mediaService)
        {
            _filesService = filesService;
            _mediaService = mediaService;
        }

        public MediaViewModel GetTransient(StorageFile file)
        {
            return new MediaViewModel(_filesService, _mediaService, file);
        }

        public MediaViewModel GetTransient(Uri uri)
        {
            return new MediaViewModel(_filesService, _mediaService, uri);
        }

        public MediaViewModel GetSingleton(StorageFile file)
        {
            string path = file.Path;
            if (!_references.TryGetValue(path, out WeakReference<MediaViewModel> reference) ||
                !reference.TryGetTarget(out MediaViewModel instance))
            {
                instance = new MediaViewModel(_filesService, _mediaService, file);
                if (!string.IsNullOrEmpty(path))
                {
                    _references[path] = new WeakReference<MediaViewModel>(instance);
                    CleanUpStaleReferences();
                }
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

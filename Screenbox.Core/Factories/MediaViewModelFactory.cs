using LibVLCSharp.Shared;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Factories
{
    public sealed class MediaViewModelFactory
    {
        private readonly IFilesService _filesService;
        private readonly IMediaService _mediaService;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly Dictionary<string, WeakReference<MediaViewModel>> _references = new();
        private int _referencesCleanUpThreshold = 1000;

        public MediaViewModelFactory(IFilesService filesService, IMediaService mediaService,
            ArtistViewModelFactory artistFactory, AlbumViewModelFactory albumFactory)
        {
            _filesService = filesService;
            _mediaService = mediaService;
            _artistFactory = artistFactory;
            _albumFactory = albumFactory;
        }

        public MediaViewModel GetTransient(IStorageFile file)
        {
            return new MediaViewModel(_filesService, _mediaService, _albumFactory, _artistFactory, file);
        }

        public MediaViewModel GetTransient(Uri uri)
        {
            return new MediaViewModel(_filesService, _mediaService, _albumFactory, _artistFactory, uri);
        }

        public MediaViewModel GetTransient(Media media)
        {
            return new MediaViewModel(_filesService, _mediaService, _albumFactory, _artistFactory, media);
        }

        public MediaViewModel GetSingleton(IStorageFile file)
        {
            string path = file.Path;
            if (!_references.TryGetValue(path, out WeakReference<MediaViewModel> reference) ||
                !reference.TryGetTarget(out MediaViewModel instance))
            {
                instance = new MediaViewModel(_filesService, _mediaService, _albumFactory, _artistFactory, file);
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

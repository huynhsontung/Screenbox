#nullable enable

using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services
{
    public sealed class LibraryService : ILibraryService
    {
        public event TypedEventHandler<ILibraryService, object>? MusicLibraryContentChanged;
        public event TypedEventHandler<ILibraryService, object>? VideosLibraryContentChanged;

        public StorageLibrary? MusicLibrary { get; private set; }
        public StorageLibrary? VideosLibrary { get; private set; }

        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly object _lockObject;

        private const int MaxLoadCount = 5000;

        private Task<MusicLibraryFetchResult>? _loadMusicTask;
        private Task<IReadOnlyList<MediaViewModel>>? _loadVideosTask;
        private List<MediaViewModel> _songs;
        private List<AlbumViewModel> _albums;
        private List<ArtistViewModel> _artists;
        private List<MediaViewModel> _videos;
        private StorageFileQueryResult? _musicLibraryQueryResult;
        private StorageFileQueryResult? _videosLibraryQueryResult;
        private bool _invalidateMusicCache;
        private bool _invalidateVideosCache;

        public LibraryService(IFilesService filesService, MediaViewModelFactory mediaFactory,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            _lockObject = new object();
            _songs = new List<MediaViewModel>();
            _albums = new List<AlbumViewModel>();
            _artists = new List<ArtistViewModel>();
            _videos = new List<MediaViewModel>();
        }

        public MusicLibraryFetchResult GetMusicCache()
        {
            return new MusicLibraryFetchResult(_songs.AsReadOnly(), _albums.AsReadOnly(), _artists.AsReadOnly(),
                _albumFactory.UnknownAlbum, _artistFactory.UnknownArtist);
        }

        public IReadOnlyList<MediaViewModel> GetVideosCache()
        {
            return _videos.AsReadOnly();
        }

        public Task<MusicLibraryFetchResult> FetchMusicAsync(bool useCache = true)
        {
            lock (_lockObject)
            {
                if (_loadMusicTask is { IsCompleted: false })
                {
                    return _loadMusicTask;
                }

                return _loadMusicTask = FetchMusicInternalAsync(useCache);
            }
        }

        public Task<IReadOnlyList<MediaViewModel>> FetchVideosAsync(bool useCache = true)
        {
            lock (_lockObject)
            {
                if (_loadVideosTask is { IsCompleted: false })
                {
                    return _loadVideosTask;
                }

                return _loadVideosTask = FetchVideosInternalAsync(useCache);
            }
        }

        public void RemoveMedia(MediaViewModel media)
        {
            if (media.Album != null)
            {
                media.Album.RelatedSongs.Remove(media);
                media.Album = null;
            }

            foreach (ArtistViewModel artist in media.Artists)
            {
                artist.RelatedSongs.Remove(media);
            }

            media.Artists = Array.Empty<ArtistViewModel>();
            _songs.Remove(media);
            _videos.Remove(media);
        }

        private async Task<MusicLibraryFetchResult> FetchMusicInternalAsync(bool useCache)
        {
            if (useCache && !_invalidateMusicCache && _musicLibraryQueryResult != null)
            {
                return GetMusicCache();
            }

            await InitializeMusicLibraryAsync();
            StorageFileQueryResult queryResult = GetMusicLibraryQueryResult();
            List<MediaViewModel> songs = await FetchMediaFromStorage(queryResult);
            _invalidateMusicCache = false;
            _songs = songs;
            _artists = _artistFactory.GetAllArtists();
            _albums = _albumFactory.GetAllAlbums();
            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
            return new MusicLibraryFetchResult(songs.AsReadOnly(), _albums.AsReadOnly(), _artists.AsReadOnly(),
                _albumFactory.UnknownAlbum, _artistFactory.UnknownArtist);
        }

        private async Task<IReadOnlyList<MediaViewModel>> FetchVideosInternalAsync(bool useCache)
        {
            if (useCache && !_invalidateVideosCache && _videosLibraryQueryResult != null)
            {
                return GetVideosCache();
            }

            await InitializeVideosLibraryAsync();
            StorageFileQueryResult queryResult = GetVideosLibraryQueryResult();
            List<MediaViewModel> videos = await FetchMediaFromStorage(queryResult);
            _invalidateVideosCache = false;
            _videos = videos;
            VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
            return videos.AsReadOnly();
        }

        private async Task<List<MediaViewModel>> FetchMediaFromStorage(StorageFileQueryResult queryResult)
        {
            List<MediaViewModel> media = new();
            uint fetchIndex = 0;
            uint count;
            try
            {
                count = await queryResult.GetItemCountAsync();
            }
            catch (Exception)
            {
                count = 0;
            }

            if (count == 0)
                return media;

            while (fetchIndex < MaxLoadCount)
            {
                IReadOnlyList<StorageFile> files = await queryResult.GetFilesAsync(fetchIndex, 50);
                if (files.Count == 0) break;
                fetchIndex += (uint)files.Count;
                media.AddRange(files.Select(_mediaFactory.GetSingleton));
            }

            foreach (MediaViewModel song in media)
            {
                await song.LoadDetailsAsync();
            }

            return media;
        }

        private async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            if (MusicLibrary == null)
            {
                MusicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                MusicLibrary.DefinitionChanged += OnMusicLibraryContentChanged;
            }

            return MusicLibrary;
        }

        private async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            if (VideosLibrary == null)
            {
                VideosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
                VideosLibrary.DefinitionChanged += OnVideosLibraryContentChanged;
            }

            return VideosLibrary;
        }

        private StorageFileQueryResult GetMusicLibraryQueryResult()
        {
            if (_musicLibraryQueryResult == null)
            {
                _musicLibraryQueryResult = _filesService.GetSongsFromLibrary();
                _musicLibraryQueryResult.ContentsChanged += OnMusicLibraryContentChanged;
            }

            return _musicLibraryQueryResult;
        }

        private StorageFileQueryResult GetVideosLibraryQueryResult()
        {
            if (_videosLibraryQueryResult == null)
            {
                _videosLibraryQueryResult = _filesService.GetVideosFromLibrary();
                _videosLibraryQueryResult.ContentsChanged += OnVideosLibraryContentChanged;
            }

            return _videosLibraryQueryResult;
        }

        private void OnVideosLibraryContentChanged(object sender, object args)
        {
            _invalidateVideosCache = true;
            VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnMusicLibraryContentChanged(object sender, object args)
        {
            _invalidateMusicCache = true;
            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

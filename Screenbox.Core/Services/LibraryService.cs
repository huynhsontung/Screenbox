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
        public bool IsLoadingVideos { get; private set; }
        public bool IsLoadingMusic { get; private set; }

        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly object _lockObject;

        private const int MaxLoadCount = 5000;

        private Task<MusicLibraryFetchResult>? _loadMusicTask;
        private List<MediaViewModel> _songs;
        private List<MediaViewModel> _videos;
        private StorageFileQueryResult? _musicLibraryQueryResult;
        private StorageFileQueryResult? _videosLibraryQueryResult;
        private bool _invalidateMusicCache;

        public LibraryService(IFilesService filesService, MediaViewModelFactory mediaFactory,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            _lockObject = new object();
            _songs = new List<MediaViewModel>();
            _videos = new List<MediaViewModel>();
            _invalidateMusicCache = true;
        }

        public async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            if (MusicLibrary == null)
            {
                MusicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                MusicLibrary.DefinitionChanged += OnMusicLibraryContentChanged;
            }

            return MusicLibrary;
        }

        public async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            if (VideosLibrary == null)
            {
                VideosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
                VideosLibrary.DefinitionChanged += OnVideosLibraryContentChanged;
            }

            return VideosLibrary;
        }

        public MusicLibraryFetchResult GetMusicCache()
        {
            return new MusicLibraryFetchResult(_songs.AsReadOnly(), _albumFactory.AllAlbums.ToList(), _artistFactory.AllArtists.ToList(),
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

        public async IAsyncEnumerable<MediaViewModel> FetchVideosAsync()
        {
            await InitializeVideosLibraryAsync();
            StorageFileQueryResult queryResult = GetVideosLibraryQueryResult();
            List<MediaViewModel> videos = new();
            while (videos.Count < MaxLoadCount)
            {
                List<MediaViewModel> videosBatch = await FetchMediaFromStorage(queryResult, (uint)videos.Count);
                if (videosBatch.Count == 0) break;
                videos.AddRange(videosBatch);

                foreach (MediaViewModel video in videosBatch)
                {
                    await video.LoadDetailsAsync();
                    yield return video;
                }
            }

            _videos = videos;
            VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task PrefetchMusicAsync()
        {
            IsLoadingMusic = true;
            try
            {
                await InitializeMusicLibraryAsync();
                StorageFileQueryResult queryResult = GetMusicLibraryQueryResult();
                List<MediaViewModel> songs = new();
                _songs = songs;
                while (songs.Count < MaxLoadCount)
                {
                    List<MediaViewModel> songsBatch = await FetchMediaFromStorage(queryResult, (uint)songs.Count);
                    if (songsBatch.Count == 0) break;
                    songs.AddRange(songsBatch);
                }

                foreach (MediaViewModel song in songs)
                {
                    await song.LoadDetailsAsync();
                }
            }
            finally
            {
                IsLoadingMusic = false;
            }

            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task PrefetchVideosAsync()
        {
            IsLoadingVideos = true;
            try
            {
                await InitializeVideosLibraryAsync();
                StorageFileQueryResult queryResult = GetVideosLibraryQueryResult();
                List<MediaViewModel> videos = new();
                _videos = videos;
                while (videos.Count < MaxLoadCount)
                {
                    List<MediaViewModel> videosBatch = await FetchMediaFromStorage(queryResult, (uint)videos.Count);
                    if (videosBatch.Count == 0) break;
                    videos.AddRange(videosBatch);
                }

                foreach (MediaViewModel video in videos)
                {
                    await video.LoadDetailsAsync();
                }
            }
            finally
            {
                IsLoadingVideos = false;
            }

            VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
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
            if (useCache && !_invalidateMusicCache)
            {
                return GetMusicCache();
            }

            await InitializeMusicLibraryAsync();
            StorageFileQueryResult queryResult = GetMusicLibraryQueryResult();
            List<MediaViewModel> songs = new();
            while (songs.Count < MaxLoadCount)
            {
                List<MediaViewModel> songsBatch = await FetchMediaFromStorage(queryResult, (uint)songs.Count);
                if (songsBatch.Count == 0) break;
                songs.AddRange(songsBatch);
            }

            _invalidateMusicCache = false;
            _songs = songs;
            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
            return new MusicLibraryFetchResult(songs.AsReadOnly(), _albumFactory.AllAlbums.ToList(), _artistFactory.AllArtists.ToList(),
                _albumFactory.UnknownAlbum, _artistFactory.UnknownArtist);
        }

        private async Task<List<MediaViewModel>> FetchMediaFromStorage(StorageFileQueryResult queryResult, uint fetchIndex, uint batchSize = 50)
        {
            IReadOnlyList<StorageFile> files;
            try
            {
                files = await queryResult.GetFilesAsync(fetchIndex, batchSize);
            }
            catch (Exception e)
            {
                files = Array.Empty<StorageFile>();
                LogService.Log(e);
            }

            List<MediaViewModel> mediaBatch = files.Select(_mediaFactory.GetSingleton).ToList();
            return mediaBatch;
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

        private async void OnVideosLibraryContentChanged(object sender, object args)
        {
            await PrefetchVideosAsync();
        }

        private async void OnMusicLibraryContentChanged(object sender, object args)
        {
            await PrefetchMusicAsync();
        }
    }
}

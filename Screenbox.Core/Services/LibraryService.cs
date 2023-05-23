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
using Windows.System;
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
        private readonly DispatcherQueue _dispatcherQueue;  // TODO: Refactor away the need for DispatcherQueue

        private const int MaxLoadCount = 5000;

        private List<MediaViewModel> _songs;
        private List<MediaViewModel> _videos;
        private StorageFileQueryResult? _musicLibraryQueryResult;
        private StorageFileQueryResult? _videosLibraryQueryResult;

        public LibraryService(IFilesService filesService, MediaViewModelFactory mediaFactory,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _songs = new List<MediaViewModel>();
            _videos = new List<MediaViewModel>();
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

        public MusicLibraryFetchResult GetMusicFetchResult()
        {
            return new MusicLibraryFetchResult(_songs.AsReadOnly(), _albumFactory.AllAlbums.ToList(), _artistFactory.AllArtists.ToList(),
                _albumFactory.UnknownAlbum, _artistFactory.UnknownArtist);
        }

        public IReadOnlyList<MediaViewModel> GetVideosFetchResult()
        {
            return _videos.AsReadOnly();
        }

        public async Task FetchMusicAsync()
        {
            if (IsLoadingMusic) return;
            IsLoadingMusic = true;
            try
            {
                await InitializeMusicLibraryAsync();
                StorageFileQueryResult queryResult = GetMusicLibraryQueryResult();
                List<MediaViewModel> songs = new();
                _songs = songs;
                await BatchFetchMediaAsync(queryResult, songs);
            }
            finally
            {
                IsLoadingMusic = false;
            }

            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task FetchVideosAsync()
        {
            if (IsLoadingVideos) return;
            IsLoadingVideos = true;
            try
            {
                await InitializeVideosLibraryAsync();
                StorageFileQueryResult queryResult = GetVideosLibraryQueryResult();
                List<MediaViewModel> videos = new();
                _videos = videos;
                await BatchFetchMediaAsync(queryResult, videos);
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

        private async Task BatchFetchMediaAsync(StorageFileQueryResult queryResult, List<MediaViewModel> target)
        {
            // Use count to stabilize query result
            uint count = await queryResult.GetItemCountAsync();

            while (target.Count < MaxLoadCount)
            {
                List<MediaViewModel> batch = await FetchMediaFromStorage(queryResult, (uint)target.Count);
                if (batch.Count == 0) break;
                target.AddRange(batch);
            }

            foreach (MediaViewModel media in target)
            {
                // Expect UI thread
                await media.LoadDetailsAsync();
            }
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

        private void OnVideosLibraryContentChanged(object sender, object args)
        {
            async void FetchAction() => await FetchVideosAsync();
            _dispatcherQueue.TryEnqueue(FetchAction);
        }

        private void OnMusicLibraryContentChanged(object sender, object args)
        {
            async void FetchAction() => await FetchMusicAsync();
            _dispatcherQueue.TryEnqueue(FetchAction);
        }
    }
}

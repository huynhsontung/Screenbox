#nullable enable

using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly StorageFileQueryResult _musicLibraryQueryResult;
        private readonly StorageFileQueryResult _videosLibraryQueryResult;
        private readonly DispatcherQueueTimer _musicRefreshTimer;
        private readonly DispatcherQueueTimer _videosRefreshTimer;

        private const int MaxLoadCount = 5000;

        private List<MediaViewModel> _songs;
        private List<MediaViewModel> _videos;
        private CancellationTokenSource? _musicFetchCts;
        private CancellationTokenSource? _videosFetchCts;

        public LibraryService(IFilesService filesService, MediaViewModelFactory mediaFactory,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _musicRefreshTimer = dispatcherQueue.CreateTimer();
            _videosRefreshTimer = dispatcherQueue.CreateTimer();
            _songs = new List<MediaViewModel>();
            _videos = new List<MediaViewModel>();

            // Init library queries
            _musicLibraryQueryResult = filesService.GetSongsFromLibrary();
            _musicLibraryQueryResult.ContentsChanged += OnMusicLibraryContentChanged;
            _videosLibraryQueryResult = filesService.GetVideosFromLibrary();
            _videosLibraryQueryResult.ContentsChanged += OnVideosLibraryContentChanged;
        }

        public async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            return MusicLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
        }

        public async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            return VideosLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
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
            _musicFetchCts?.Cancel();
            using CancellationTokenSource cts = new();
            _musicFetchCts = cts;
            try
            {
                await FetchMusicCancelableAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _musicFetchCts = null;
        }

        public async Task FetchVideosAsync()
        {
            _videosFetchCts?.Cancel();
            using CancellationTokenSource cts = new();
            _videosFetchCts = cts;
            try
            {
                await FetchVideosCancelableAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _videosFetchCts = null;
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

        private async Task FetchMusicCancelableAsync(CancellationToken cancellationToken)
        {
            IsLoadingMusic = true;
            try
            {
                await InitializeMusicLibraryAsync();
                cancellationToken.ThrowIfCancellationRequested();
                List<MediaViewModel> songs = new();
                _songs = songs;
                await BatchFetchMediaAsync(_musicLibraryQueryResult, songs, cancellationToken);
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsLoadingMusic = false;
                }
            }

            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task FetchVideosCancelableAsync(CancellationToken cancellationToken)
        {
            IsLoadingVideos = true;
            try
            {
                await InitializeVideosLibraryAsync();
                cancellationToken.ThrowIfCancellationRequested();
                List<MediaViewModel> videos = new();
                _videos = videos;
                await BatchFetchMediaAsync(_videosLibraryQueryResult, videos, cancellationToken);
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsLoadingVideos = false;
                }
            }

            VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task BatchFetchMediaAsync(StorageFileQueryResult queryResult, List<MediaViewModel> target, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (target.Count < MaxLoadCount)
            {
                List<MediaViewModel> batch = await FetchMediaFromStorage(queryResult, (uint)target.Count);
                if (batch.Count == 0) break;
                target.AddRange(batch);
                cancellationToken.ThrowIfCancellationRequested();
            }

            foreach (MediaViewModel media in target)
            {
                // Expect UI thread
                await media.LoadDetailsAsync();
                cancellationToken.ThrowIfCancellationRequested();
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

        private void OnVideosLibraryContentChanged(object sender, object args)
        {
            async void FetchAction() => await FetchVideosAsync();
            // Delay fetch due to query result not yet updated at this time
            _videosRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(500));
        }

        private void OnMusicLibraryContentChanged(object sender, object args)
        {
            async void FetchAction() => await FetchMusicAsync();
            // Delay fetch due to query result not yet updated at this time
            _musicRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(500));
        }
    }
}

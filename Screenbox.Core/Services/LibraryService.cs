#nullable enable

using CommunityToolkit.WinUI;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
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
        private bool UseIndexer => _settingsService.UseIndexer;

        private readonly ISettingsService _settingsService;
        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly DispatcherQueueTimer _musicRefreshTimer;
        private readonly DispatcherQueueTimer _videosRefreshTimer;

        private StorageFileQueryResult? _musicLibraryQueryResult;
        private StorageFileQueryResult? _videosLibraryQueryResult;
        private List<MediaViewModel> _songs;
        private List<MediaViewModel> _videos;
        private CancellationTokenSource? _musicFetchCts;
        private CancellationTokenSource? _videosFetchCts;

        private const string SongsCacheFileName = "songs.bin";

        public LibraryService(ISettingsService settingsService, IFilesService filesService,
            MediaViewModelFactory mediaFactory, AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _settingsService = settingsService;
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _musicRefreshTimer = dispatcherQueue.CreateTimer();
            _videosRefreshTimer = dispatcherQueue.CreateTimer();
            _songs = new List<MediaViewModel>();
            _videos = new List<MediaViewModel>();
        }

        public async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            // No need to add handler for StorageLibrary.DefinitionChanged
            return MusicLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
        }

        public async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            // No need to add handler for StorageLibrary.DefinitionChanged
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

        private async Task CacheSongsAsync(CancellationToken cancellationToken)
        {
            List<PersistentSongRecord> records = _songs.Select(song =>
                new PersistentSongRecord(song.Name, song.Location, song.MediaInfo.MusicProperties)).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            await _filesService.SaveToDiskAsync(ApplicationData.Current.LocalFolder, SongsCacheFileName, records);
        }

        private async Task<List<MediaViewModel>> LoadSongsCacheAsync(CancellationToken cancellationToken)
        {
            List<PersistentSongRecord> records;
            try
            {
                records = await _filesService.LoadFromDiskAsync<List<PersistentSongRecord>>(ApplicationData.Current.LocalFolder, SongsCacheFileName);
            }
            catch (Exception)
            {
                // FileNotFoundException
                // UnauthorizedAccessException
                return new List<MediaViewModel>();
            }

            cancellationToken.ThrowIfCancellationRequested();
            List<MediaViewModel> songs = records.Select(record =>
            {
                MediaViewModel media = _mediaFactory.GetSingleton(new Uri(record.Path));
                media.IsFromLibrary = true;
                media.Name = record.Title;
                media.MediaInfo = new MediaInfo(record.Properties);
                return media;
            }).ToList();
            return songs;
        }

        private async Task FetchMusicCancelableAsync(CancellationToken cancellationToken)
        {
            IsLoadingMusic = true;
            try
            {
                StorageFileQueryResult libraryQuery = GetMusicLibraryQuery();
                await InitializeMusicLibraryAsync();
                cancellationToken.ThrowIfCancellationRequested();
                List<MediaViewModel> songs = _songs = await LoadSongsCacheAsync(cancellationToken);
                // If cache is empty, fetch from storage using the same songs instance
                // If not empty then create a new list to avoid overwriting the cache
                if (songs.Count > 0) songs = new List<MediaViewModel>();
                await BatchFetchMediaAsync(libraryQuery, songs, cancellationToken);
                _songs = songs;
            }
            catch (Exception e)
            {
                if (e.HResult == unchecked((int)0x80270200))    // LIBRARY_E_NO_SAVE_LOCATION
                {
                    LogService.Log(e);
                    return;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsLoadingMusic = false;
                }
            }

            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
            await CacheSongsAsync(cancellationToken);
        }

        private async Task FetchVideosCancelableAsync(CancellationToken cancellationToken)
        {
            IsLoadingVideos = true;
            try
            {
                StorageFileQueryResult libraryQuery = GetVideosLibraryQuery();
                await InitializeVideosLibraryAsync();
                cancellationToken.ThrowIfCancellationRequested();
                List<MediaViewModel> videos = new();
                _videos = videos;
                await BatchFetchMediaAsync(libraryQuery, videos, cancellationToken);
            }
            catch (Exception e)
            {
                if (e.HResult == unchecked((int)0x80270200))    // LIBRARY_E_NO_SAVE_LOCATION
                {
                    LogService.Log(e);
                    return;
                }
                else
                {
                    throw;
                }
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
            while (true)
            {
                List<MediaViewModel> batch = await FetchMediaFromStorage(queryResult, (uint)target.Count);
                if (batch.Count == 0) break;
                target.AddRange(batch);
                cancellationToken.ThrowIfCancellationRequested();
            }

            foreach (MediaViewModel media in target)
            {
                // Expect UI thread
                media.IsFromLibrary = true;
                await media.LoadDetailsAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private StorageFileQueryResult GetMusicLibraryQuery()
        {
            StorageFileQueryResult? libraryQuery = _musicLibraryQueryResult;

            if (libraryQuery != null && ShouldUpdateQuery(libraryQuery, UseIndexer))
            {
                libraryQuery.ContentsChanged -= OnMusicLibraryContentChanged;
                libraryQuery = null;
            }

            if (libraryQuery == null)
            {
                libraryQuery = CreateMusicLibraryQuery(UseIndexer);
                libraryQuery.ContentsChanged += OnMusicLibraryContentChanged;
            }

            _musicLibraryQueryResult = libraryQuery;
            return libraryQuery;
        }

        private StorageFileQueryResult GetVideosLibraryQuery()
        {
            StorageFileQueryResult? libraryQuery = _videosLibraryQueryResult;

            if (libraryQuery != null && ShouldUpdateQuery(libraryQuery, UseIndexer))
            {
                libraryQuery.ContentsChanged -= OnVideosLibraryContentChanged;
                libraryQuery = null;
            }

            if (libraryQuery == null)
            {
                libraryQuery = CreateVideosLibraryQuery(UseIndexer);
                libraryQuery.ContentsChanged += OnVideosLibraryContentChanged;
            }

            _videosLibraryQueryResult = libraryQuery;
            return libraryQuery;
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
                e.Data[nameof(fetchIndex)] = fetchIndex;
                e.Data[nameof(batchSize)] = batchSize;
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

        private static bool ShouldUpdateQuery(IStorageQueryResultBase query, bool useIndexer)
        {
            QueryOptions options = query.GetCurrentQueryOptions();
            bool agree1 = !useIndexer && options.IndexerOption == IndexerOption.DoNotUseIndexer;
            bool agree2 = useIndexer && options.IndexerOption != IndexerOption.DoNotUseIndexer;
            return !agree1 && !agree2;
        }

        private static StorageFileQueryResult CreateMusicLibraryQuery(bool useIndexer)
        {
            string[] customPropertyKeys =
            {
                SystemProperties.Title,
                SystemProperties.Music.Artist,
                SystemProperties.Media.Duration
            };

            QueryOptions queryOptions = new(CommonFileQuery.OrderByTitle, FilesHelpers.SupportedAudioFormats)
            {
                IndexerOption = useIndexer ? IndexerOption.UseIndexerWhenAvailable : IndexerOption.DoNotUseIndexer
            };
            queryOptions.SetPropertyPrefetch(
                PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.MusicProperties,
                customPropertyKeys);

            return KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOptions);
        }

        private static StorageFileQueryResult CreateVideosLibraryQuery(bool useIndexer)
        {
            string[] customPropertyKeys =
            {
                SystemProperties.Title,
                SystemProperties.Media.Duration
            };

            QueryOptions queryOptions = new(CommonFileQuery.OrderByName, FilesHelpers.SupportedVideoFormats)
            {
                IndexerOption = useIndexer ? IndexerOption.UseIndexerWhenAvailable : IndexerOption.DoNotUseIndexer
            };
            queryOptions.SetPropertyPrefetch(
                PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.VideoProperties,
                customPropertyKeys);
            return KnownFolders.VideosLibrary.CreateFileQueryWithOptions(queryOptions);
        }
    }
}

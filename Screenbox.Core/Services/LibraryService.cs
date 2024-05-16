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
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.System;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services
{
    // TODO: Break this service into smaller ViewModels and services
    public sealed class LibraryService : ILibraryService
    {
        public event TypedEventHandler<ILibraryService, object>? MusicLibraryContentChanged;
        public event TypedEventHandler<ILibraryService, object>? VideosLibraryContentChanged;

        public StorageLibrary? MusicLibrary { get; private set; }
        public StorageLibrary? VideosLibrary { get; private set; }
        public bool IsLoadingVideos { get; private set; }
        public bool IsLoadingMusic { get; private set; }
        private bool UseIndexer => _settingsService.UseIndexer;
        private bool SearchRemovableStorage => _settingsService.SearchRemovableStorage && SystemInformation.IsXbox;

        private static readonly string[] CustomPropertyKeys = { SystemProperties.Title };
        private readonly ISettingsService _settingsService;
        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly DispatcherQueueTimer _musicRefreshTimer;
        private readonly DispatcherQueueTimer _videosRefreshTimer;
        private readonly DispatcherQueueTimer _storageDeviceRefreshTimer;
        private readonly DeviceWatcher? _portableStorageDeviceWatcher;

        private StorageFileQueryResult? _musicLibraryQueryResult;
        private StorageFileQueryResult? _videosLibraryQueryResult;
        private List<MediaViewModel> _songs;
        private List<MediaViewModel> _videos;
        private CancellationTokenSource? _musicFetchCts;
        private CancellationTokenSource? _videosFetchCts;

        private const string SongsCacheFileName = "songs.bin";
        private const string VideoCacheFileName = "videos.bin";

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
            _storageDeviceRefreshTimer = dispatcherQueue.CreateTimer();
            _songs = new List<MediaViewModel>();
            _videos = new List<MediaViewModel>();

            if (SystemInformation.IsXbox)
            {
                _portableStorageDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
                _portableStorageDeviceWatcher.Removed += OnPortableStorageDeviceChanged;
                _portableStorageDeviceWatcher.Updated += OnPortableStorageDeviceChanged;
            }
        }

        public async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            // No need to add handler for StorageLibrary.DefinitionChanged
            MusicLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            MusicLibrary.ChangeTracker.Enable();
            return MusicLibrary;
        }

        public async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            // No need to add handler for StorageLibrary.DefinitionChanged
            VideosLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            VideosLibrary.ChangeTracker.Enable();
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

        public async Task FetchMusicAsync(bool useCache = true)
        {
            _musicFetchCts?.Cancel();
            using CancellationTokenSource cts = new();
            _musicFetchCts = cts;
            try
            {
                await InitializeMusicLibraryAsync();
                cts.Token.ThrowIfCancellationRequested();
                await FetchMusicCancelableAsync(useCache, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                if (e.HResult == unchecked((int)0x80270200)) // LIBRARY_E_NO_SAVE_LOCATION
                {
                    LogService.Log(e);
                    return;
                }

                throw;
            }
            finally
            {
                _musicFetchCts = null;
            }
        }

        public async Task FetchVideosAsync(bool useCache = true)
        {
            _videosFetchCts?.Cancel();
            using CancellationTokenSource cts = new();
            _videosFetchCts = cts;
            try
            {
                await InitializeVideosLibraryAsync();
                cts.Token.ThrowIfCancellationRequested();
                await FetchVideosCancelableAsync(useCache, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                if (e.HResult == unchecked((int)0x80270200)) // LIBRARY_E_NO_SAVE_LOCATION
                {
                    LogService.Log(e);
                    return;
                }

                throw;
            }
            finally
            {
                _videosFetchCts = null;
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

        private async Task CacheSongsAsync(CancellationToken cancellationToken)
        {
            var folderPaths = MusicLibrary!.Folders.Select(f => f.Path).ToList();
            var records = _songs.Select(song =>
                new PersistentSongRecord(song.Name, song.Location, song.MediaInfo.MusicProperties)).ToList();
            var libraryCache = new PersistentMusicLibrary
            {
                FolderPaths = folderPaths,
                SongRecords = records
            };
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _filesService.SaveToDiskAsync(ApplicationData.Current.LocalFolder, SongsCacheFileName, libraryCache);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async Task CacheVideosAsync(CancellationToken cancellationToken)
        {
            var folderPaths = VideosLibrary!.Folders.Select(f => f.Path).ToList();
            List<PersistentVideoRecord> records = _videos.Select(video =>
                           new PersistentVideoRecord(video.Name, video.Location, video.MediaInfo.VideoProperties)).ToList();
            var libraryCache = new PersistentVideoLibrary()
            {
                FolderPaths = folderPaths,
                VideoRecords = records
            };
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _filesService.SaveToDiskAsync(ApplicationData.Current.LocalFolder, VideoCacheFileName, libraryCache);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async Task<PersistentMusicLibrary?> LoadMusicLibraryCacheAsync()
        {
            try
            {
                return await _filesService.LoadFromDiskAsync<PersistentMusicLibrary>(
                    ApplicationData.Current.LocalFolder, SongsCacheFileName);
            }
            catch (Exception)
            {
                // FileNotFoundException
                // UnauthorizedAccessException
                // and other Protobuf exceptions
                // Deserialization failed
                return null;
            }
        }

        private List<MediaViewModel> GetSongsFromCache(PersistentMusicLibrary libraryCache)
        {
            var records = libraryCache.SongRecords;
            List<MediaViewModel> songs = records.Select(record =>
            {
                MediaViewModel media = _mediaFactory.GetSingleton(new Uri(record.Path));
                media.IsFromLibrary = true;
                if (!string.IsNullOrEmpty(record.Title)) media.Name = record.Title;
                media.MediaInfo = new MediaInfo(record.Properties);
                return media;
            }).ToList();
            return songs;
        }

        private async Task<PersistentVideoLibrary?> LoadVideosLibraryCacheAsync()
        {
            try
            {
                return await _filesService.LoadFromDiskAsync<PersistentVideoLibrary>(
                    ApplicationData.Current.LocalFolder, VideoCacheFileName);
            }
            catch (Exception)
            {
                // FileNotFoundException
                // UnauthorizedAccessException
                // and other Protobuf exceptions
                // Deserialization failed
                return null;
            }
        }

        private List<MediaViewModel> GetVideosFromCache(PersistentVideoLibrary libraryCache)
        {
            var records = libraryCache.VideoRecords;
            List<MediaViewModel> videos = records.Select(record =>
            {
                MediaViewModel media = _mediaFactory.GetSingleton(new Uri(record.Path));
                media.IsFromLibrary = true;
                if (!string.IsNullOrEmpty(record.Title)) media.Name = record.Title;
                media.MediaInfo = new MediaInfo(record.Properties);
                return media;
            }).ToList();
            return videos;
        }

        private async Task FetchMusicCancelableAsync(bool useCache, CancellationToken cancellationToken)
        {
            if (MusicLibrary == null) return;
            IsLoadingMusic = true;

            useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
            bool hasCache = false;
            var libraryChangeTracker = MusicLibrary.ChangeTracker;
            libraryChangeTracker.Enable();
            var changeReader = libraryChangeTracker.GetChangeReader();
            try
            {
                var libraryQuery = GetMusicLibraryQuery();
                List<MediaViewModel> songs = new();
                if (useCache)
                {
                    var libraryCache = await LoadMusicLibraryCacheAsync();
                    if (libraryCache?.SongRecords.Count > 0)
                    {
                        songs = GetSongsFromCache(libraryCache);
                        hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, MusicLibrary);

                        // Update cache with changes from library tracker. Invalidate cache if needed.
                        if (ApiInformation.IsMethodPresent("Windows.Storage.StorageLibraryChangeReader",
                                "GetLastChangeId"))
                        {
                            var changeId = changeReader.GetLastChangeId();
                            if (changeId == StorageLibraryLastChangeId.Unknown)
                            {
                                hasCache = false;
                            }
                            else if (changeId > 0)
                            {
                                hasCache = await TryResolveLibraryChangeAsync(songs, changeReader);
                            }
                        }
                        else
                        {
                            hasCache = await TryResolveLibraryChangeAsync(songs, changeReader);
                        }
                    }
                }

                // Recrawl the library if there is no cache or cache is invalidated
                if (!hasCache)
                {
                    _songs = songs;
                    songs.Clear();
                    _albumFactory.Clear();
                    _artistFactory.Clear();
                    await BatchFetchMediaAsync(libraryQuery, songs, cancellationToken);

                    // Search removable storage if the system is Xbox
                    if (SearchRemovableStorage)
                    {
                        libraryQuery = CreateRemovableStorageMusicQuery();
                        await BatchFetchMediaAsync(libraryQuery, songs, cancellationToken);
                        StartPortableStorageDeviceWatcher();
                    }
                }

                if (songs != _songs)
                {
                    // Ensure only songs not in the library has IsFromLibrary = false
                    _songs.ForEach(song => song.IsFromLibrary = false);
                    songs.ForEach(song => song.IsFromLibrary = true);
                    CleanOutdatedSongs();
                }

                // Populate Album and Artists for each song
                foreach (MediaViewModel song in songs)
                {
                    // A cached song always has a URI as source
                    if (hasCache && song.Source is Uri)
                    {
                        song.UpdateAlbum(_albumFactory);
                        song.UpdateArtists(_artistFactory);
                    }
                    else
                    {
                        await song.LoadDetailsAsync(_filesService);
                        cancellationToken.ThrowIfCancellationRequested();
                        song.UpdateAlbum(_albumFactory);
                        song.UpdateArtists(_artistFactory);
                    }
                }

                _songs = songs;
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
            if (hasCache)
            {
                await changeReader.AcceptChangesAsync();
            }
            else
            {
                libraryChangeTracker.Reset();
            }
        }

        private async Task FetchVideosCancelableAsync(bool useCache, CancellationToken cancellationToken)
        {
            if (VideosLibrary == null) return;
            IsLoadingVideos = true;

            useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
            bool hasCache = false;
            var libraryChangeTracker = VideosLibrary.ChangeTracker;
            libraryChangeTracker.Enable();
            var changeReader = libraryChangeTracker.GetChangeReader();
            try
            {
                StorageFileQueryResult libraryQuery = GetVideosLibraryQuery();
                List<MediaViewModel> videos = new();
                if (useCache)
                {
                    var libraryCache = await LoadVideosLibraryCacheAsync();
                    if (libraryCache?.VideoRecords.Count > 0)
                    {
                        videos = GetVideosFromCache(libraryCache);
                        hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, VideosLibrary);

                        // Update cache with changes from library tracker. Invalidate cache if needed.
                        var changeId = changeReader.GetLastChangeId();
                        if (changeId == StorageLibraryLastChangeId.Unknown)
                        {
                            hasCache = false;
                        }
                        else if (changeId > 0)
                        {
                            hasCache = await TryResolveLibraryChangeAsync(videos, changeReader);
                        }
                    }
                }

                // Recrawl the library if there is no cache or cache is invalidated
                if (!hasCache)
                {
                    _videos = videos;
                    videos.Clear();
                    await BatchFetchMediaAsync(libraryQuery, videos, cancellationToken);

                    // Search removable storage if the system is Xbox
                    if (SearchRemovableStorage)
                    {
                        libraryQuery = CreateRemovableStorageVideosQuery();
                        await BatchFetchMediaAsync(libraryQuery, videos, cancellationToken);
                        StartPortableStorageDeviceWatcher();
                    }
                }

                foreach (MediaViewModel video in videos)
                {
                    video.IsFromLibrary = true;
                    if (!hasCache)
                    {
                        await video.LoadDetailsAsync(_filesService);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                _videos = videos;
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsLoadingVideos = false;
                }
            }

            VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
            await CacheVideosAsync(cancellationToken);
            if (hasCache)
            {
                await changeReader.AcceptChangesAsync();
            }
            else
            {
                libraryChangeTracker.Reset();
            }
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
        }

        /// <summary>
        /// Clean up songs that are no longer from the library
        /// </summary>
        private void CleanOutdatedSongs()
        {
            List<MediaViewModel> outdatedSongs = _songs.Where(song => !song.IsFromLibrary).ToList();
            foreach (MediaViewModel song in outdatedSongs)
            {
                if (song.Album != null)
                {
                    song.Album.RelatedSongs.Remove(song);
                    song.Album = null;
                }

                foreach (ArtistViewModel artist in song.Artists)
                {
                    artist.RelatedSongs.Remove(song);
                }

                song.Artists = Array.Empty<ArtistViewModel>();
                song.Clean();
            }

            _albumFactory.Compact();
            _artistFactory.Compact();
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
                // System.Exception: The library, drive, or media pool is empty.
                if (e.HResult != unchecked((int)0x800710D2))
                {
                    e.Data[nameof(fetchIndex)] = fetchIndex;
                    e.Data[nameof(batchSize)] = batchSize;
                    LogService.Log(e);
                }

                return new List<MediaViewModel>();
            }

            List<MediaViewModel> mediaBatch = files.Select(_mediaFactory.GetSingleton).ToList();
            return mediaBatch;
        }

        private async Task<bool> TryResolveLibraryChangeAsync(List<MediaViewModel> mediaList, StorageLibraryChangeReader changeReader)
        {
            var changeBatch = await changeReader.ReadBatchAsync();
            foreach (StorageLibraryChange change in changeBatch)
            {
                // If this is a folder change then give up
                if (change.IsOfType(StorageItemTypes.Folder) &&
                    change.ChangeType is not (StorageLibraryChangeType.IndexingStatusChanged
                        or StorageLibraryChangeType.EncryptionChanged)) return false;

                StorageFile file;
                MediaViewModel existing;
                switch (change.ChangeType)
                {
                    case StorageLibraryChangeType.Created:
                    case StorageLibraryChangeType.MovedIntoLibrary:
                        file = (StorageFile)await change.GetStorageItemAsync();
                        mediaList.Add(_mediaFactory.GetSingleton(file));
                        break;

                    case StorageLibraryChangeType.Deleted:
                    case StorageLibraryChangeType.MovedOutOfLibrary:
                        existing = mediaList.Find(s =>
                            s.Location.Equals(change.PreviousPath, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                        {
                            RemoveMedia(existing);
                            mediaList.Remove(existing);
                        }

                        break;

                    case StorageLibraryChangeType.MovedOrRenamed:
                        file = (StorageFile)await change.GetStorageItemAsync();
                        existing = mediaList.Find(s =>
                            s.Location.Equals(change.PreviousPath, StringComparison.OrdinalIgnoreCase));

                        var newMedia = _mediaFactory.GetSingleton(file);
                        if (existing != null)
                        {
                            var existingInfo = existing.MediaInfo;
                            RemoveMedia(existing);
                            mediaList.Remove(existing);
                            newMedia.MediaInfo = existingInfo;
                        }

                        mediaList.Add(newMedia);
                        break;

                    case StorageLibraryChangeType.ContentsChanged:
                    case StorageLibraryChangeType.ContentsReplaced:
                    case StorageLibraryChangeType.EncryptionChanged:
                    case StorageLibraryChangeType.IndexingStatusChanged:
                        break;
                    case StorageLibraryChangeType.ChangeTrackingLost:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return true;
        }

        private void OnVideosLibraryContentChanged(object sender, object args)
        {
            async void FetchAction() => await FetchVideosAsync();
            // Delay fetch due to query result not yet updated at this time
            _videosRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
        }

        private void OnMusicLibraryContentChanged(object sender, object args)
        {
            async void FetchAction() => await FetchMusicAsync();
            // Delay fetch due to query result not yet updated at this time
            _musicRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
        }

        private void OnPortableStorageDeviceChanged(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (!SearchRemovableStorage) return;
            async void FetchAction()
            {
                await FetchVideosAsync();
                await FetchMusicAsync();
            }
            _storageDeviceRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
        }

        private void StartPortableStorageDeviceWatcher()
        {
            if (_portableStorageDeviceWatcher?.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped)
            {
                _portableStorageDeviceWatcher.Start();
            }
        }

        private static bool AreLibraryPathsChanged(IReadOnlyCollection<string> cachedFolderPaths, StorageLibrary library)
        {
            var paths = library.Folders.Select(f => f.Path).ToList();
            if (cachedFolderPaths.Count != paths.Count) return true;
            return cachedFolderPaths.Any(cachedPath => !paths.Contains(cachedPath, StringComparer.OrdinalIgnoreCase));
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
            QueryOptions queryOptions = new(CommonFileQuery.OrderByTitle, FilesHelpers.SupportedAudioFormats)
            {
                IndexerOption = useIndexer ? IndexerOption.UseIndexerWhenAvailable : IndexerOption.DoNotUseIndexer
            };
            queryOptions.SetPropertyPrefetch(
                PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.MusicProperties,
                CustomPropertyKeys);

            return KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOptions);
        }

        private static StorageFileQueryResult CreateVideosLibraryQuery(bool useIndexer)
        {
            QueryOptions queryOptions = new(CommonFileQuery.OrderByName, FilesHelpers.SupportedVideoFormats)
            {
                IndexerOption = useIndexer ? IndexerOption.UseIndexerWhenAvailable : IndexerOption.DoNotUseIndexer
            };
            queryOptions.SetPropertyPrefetch(
                PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.VideoProperties,
                CustomPropertyKeys);
            return KnownFolders.VideosLibrary.CreateFileQueryWithOptions(queryOptions);
        }

        private static StorageFileQueryResult CreateRemovableStorageMusicQuery()
        {
            // Removable storage does not support any other default queries.
            // Other than Default and SortByName, all other queries return empty results.
            QueryOptions queryOptions = new(CommonFileQuery.OrderByName, FilesHelpers.SupportedAudioFormats);
            queryOptions.SetPropertyPrefetch(
                PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.MusicProperties,
                CustomPropertyKeys);
            return KnownFolders.RemovableDevices.CreateFileQueryWithOptions(queryOptions);
        }

        private static StorageFileQueryResult CreateRemovableStorageVideosQuery()
        {
            // Removable storage does not support any other default queries.
            // Other than Default and SortByName, all other queries return empty results.
            QueryOptions queryOptions = new(CommonFileQuery.OrderByName, FilesHelpers.SupportedVideoFormats);
            queryOptions.SetPropertyPrefetch(
                PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.VideoProperties,
                CustomPropertyKeys);
            return KnownFolders.RemovableDevices.CreateFileQueryWithOptions(queryOptions);
        }
    }
}

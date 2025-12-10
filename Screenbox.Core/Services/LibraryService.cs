#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
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

        public StorageLibrary? MusicLibrary
        {
            get => State.MusicLibrary;
            private set => State.MusicLibrary = value;
        }

        public StorageLibrary? VideosLibrary
        {
            get => State.VideosLibrary;
            private set => State.VideosLibrary = value;
        }

        public bool IsLoadingVideos
        {
            get => State.IsLoadingVideos;
            private set => State.IsLoadingVideos = value;
        }

        public bool IsLoadingMusic
        {
            get => State.IsLoadingMusic;
            private set => State.IsLoadingMusic = value;
        }
        private bool UseIndexer => _settingsService.UseIndexer;
        private bool SearchRemovableStorage => _settingsService.SearchRemovableStorage && SystemInformation.IsXbox;

        private static readonly string[] CustomPropertyKeys = { SystemProperties.Title };
        private readonly ISettingsService _settingsService;
        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly LibraryContext State;
        private readonly DispatcherQueueTimer _musicRefreshTimer;
        private readonly DispatcherQueueTimer _videosRefreshTimer;
        private readonly DispatcherQueueTimer _storageDeviceRefreshTimer;
        private readonly DeviceWatcher? _portableStorageDeviceWatcher;
        private StorageFileQueryResult? MusicLibraryQueryResult
        {
            get => State.MusicLibraryQueryResult;
            set => State.MusicLibraryQueryResult = value;
        }

        private StorageFileQueryResult? VideosLibraryQueryResult
        {
            get => State.VideosLibraryQueryResult;
            set => State.VideosLibraryQueryResult = value;
        }

        private CancellationTokenSource? MusicFetchCancellation
        {
            get => State.MusicFetchCancellation;
            set => State.MusicFetchCancellation = value;
        }

        private CancellationTokenSource? VideosFetchCancellation
        {
            get => State.VideosFetchCancellation;
            set => State.VideosFetchCancellation = value;
        }

        private bool MusicChangeTrackerAvailable
        {
            get => State.MusicChangeTrackerAvailable;
            set => State.MusicChangeTrackerAvailable = value;
        }

        private bool VideosChangeTrackerAvailable
        {
            get => State.VideosChangeTrackerAvailable;
            set => State.VideosChangeTrackerAvailable = value;
        }

        private const string SongsCacheFileName = "songs.bin";
        private const string VideoCacheFileName = "videos.bin";

        public LibraryService(ISettingsService settingsService, IFilesService filesService,
            MediaViewModelFactory mediaFactory, AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory,
            LibraryContext state)
        {
            _settingsService = settingsService;
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            State = state;
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _musicRefreshTimer = State.MusicRefreshTimer ??= dispatcherQueue.CreateTimer();
            _videosRefreshTimer = State.VideosRefreshTimer ??= dispatcherQueue.CreateTimer();
            _storageDeviceRefreshTimer = State.StorageDeviceRefreshTimer ??= dispatcherQueue.CreateTimer();

            if (SystemInformation.IsXbox)
            {
                if (State.PortableStorageDeviceWatcher != null)
                {
                    State.PortableStorageDeviceWatcher.Removed -= OnPortableStorageDeviceChanged;
                    State.PortableStorageDeviceWatcher.Updated -= OnPortableStorageDeviceChanged;
                }

                _portableStorageDeviceWatcher = State.PortableStorageDeviceWatcher ?? DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
                State.PortableStorageDeviceWatcher = _portableStorageDeviceWatcher;
                _portableStorageDeviceWatcher.Removed += OnPortableStorageDeviceChanged;
                _portableStorageDeviceWatcher.Updated += OnPortableStorageDeviceChanged;
            }
        }

        public async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            // No need to add handler for StorageLibrary.DefinitionChanged
            MusicLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            try
            {
                MusicLibrary.ChangeTracker.Enable();
                MusicChangeTrackerAvailable = true;
            }
            catch (Exception)
            {
                // pass
            }

            return MusicLibrary;
        }

        public async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            // No need to add handler for StorageLibrary.DefinitionChanged
            VideosLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            try
            {
                VideosLibrary.ChangeTracker.Enable();
                VideosChangeTrackerAvailable = true;
            }
            catch (Exception)
            {
                // pass
            }

            return VideosLibrary;
        }

        public MusicLibraryFetchResult GetMusicFetchResult()
        {
            return new MusicLibraryFetchResult(State.Songs.AsReadOnly(), _albumFactory.AllAlbums.ToList(), _artistFactory.AllArtists.ToList(),
                _albumFactory.UnknownAlbum, _artistFactory.UnknownArtist);
        }

        public IReadOnlyList<MediaViewModel> GetVideosFetchResult()
        {
            return State.Videos.AsReadOnly();
        }

        public async Task FetchMusicAsync(bool useCache = true)
        {
            MusicFetchCancellation?.Cancel();
            using CancellationTokenSource cts = new();
            MusicFetchCancellation = cts;
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
            finally
            {
                MusicFetchCancellation = null;
            }
        }

        public async Task FetchVideosAsync(bool useCache = true)
        {
            VideosFetchCancellation?.Cancel();
            using CancellationTokenSource cts = new();
            VideosFetchCancellation = cts;
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
            finally
            {
                VideosFetchCancellation = null;
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
            State.Songs.Remove(media);
            State.Videos.Remove(media);
        }

        private async Task CacheSongsAsync(CancellationToken cancellationToken)
        {
            var folderPaths = MusicLibrary!.Folders.Select(f => f.Path).ToList();
            var records = State.Songs.Select(song =>
                new PersistentMediaRecord(song.Name, song.Location, song.MediaInfo.MusicProperties, song.DateAdded)).ToList();
            var libraryCache = new PersistentStorageLibrary
            {
                FolderPaths = folderPaths,
                Records = records
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
            List<PersistentMediaRecord> records = State.Videos.Select(video =>
                           new PersistentMediaRecord(video.Name, video.Location, video.MediaInfo.VideoProperties, video.DateAdded)).ToList();
            var libraryCache = new PersistentStorageLibrary()
            {
                FolderPaths = folderPaths,
                Records = records
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

        private async Task<PersistentStorageLibrary?> LoadStorageLibraryCacheAsync(string fileName)
        {
            try
            {
                return await _filesService.LoadFromDiskAsync<PersistentStorageLibrary>(
                    ApplicationData.Current.LocalFolder, fileName);
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

        private List<MediaViewModel> GetMediaFromCache(PersistentStorageLibrary libraryCache)
        {
            var records = libraryCache.Records;
            List<MediaViewModel> mediaList = records.Select(record =>
            {
                MediaViewModel media = _mediaFactory.GetSingleton(new Uri(record.Path));
                media.IsFromLibrary = true;
                if (!string.IsNullOrEmpty(record.Title)) media.Name = record.Title;
                media.MediaInfo = new MediaInfo(record.Properties);
                if (record.DateAdded != default)
                {
                    DateTimeOffset utcTime = DateTime.SpecifyKind(record.DateAdded, DateTimeKind.Utc);
                    media.DateAdded = utcTime.ToLocalTime();
                }
                return media;
            }).ToList();
            return mediaList;
        }

        private async Task FetchMusicCancelableAsync(bool useCache, CancellationToken cancellationToken)
        {
            if (MusicLibrary == null) return;
            IsLoadingMusic = true;
            StorageLibraryChangeTracker? libraryChangeTracker = null;
            StorageLibraryChangeReader? changeReader = null;
            try
            {
                useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
                bool hasCache = false;
                await KnownFolders.RequestAccessAsync(KnownFolderId.MusicLibrary);
                var libraryQuery = GetMusicLibraryQuery();
                List<MediaViewModel> songs = new();
                List<MediaViewModel> previousSongs = State.Songs.ToList();
                if (useCache)
                {
                    var libraryCache = await LoadStorageLibraryCacheAsync(SongsCacheFileName);
                    if (libraryCache?.Records.Count > 0)
                    {
                        songs = GetMediaFromCache(libraryCache);
                        hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, MusicLibrary);

                        // Update cache with changes from library tracker. Invalidate cache if needed.
                        if (hasCache && MusicChangeTrackerAvailable)
                        {
                            try
                            {
                                libraryChangeTracker = MusicLibrary.ChangeTracker;
                                libraryChangeTracker.Enable();
                                changeReader = libraryChangeTracker.GetChangeReader();
                                hasCache = await TryResolveLibraryChangeAsync(songs, changeReader);
                            }
                            catch (Exception e)
                            {
                                LogService.Log($"Failed to resolve change from library tracker\n{e}");
                            }
                        }
                    }
                }

                // Recrawl the library if there is no cache or cache is invalidated
                if (!hasCache)
                {
                    songs.Clear();
                    _albumFactory.Clear();
                    _artistFactory.Clear();
                    await BatchFetchMediaAsync(libraryQuery, songs, cancellationToken);

                    // Search removable storage if the system is Xbox
                    if (SearchRemovableStorage)
                    {
                        var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                        if (accessStatus is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
                        {
                            libraryQuery = CreateRemovableStorageMusicQuery();
                            await BatchFetchMediaAsync(libraryQuery, songs, cancellationToken);
                            StartPortableStorageDeviceWatcher();
                        }
                    }
                }

                if (!songs.SequenceEqual(previousSongs))
                {
                    // Ensure only songs not in the library has IsFromLibrary = false
                    previousSongs.ForEach(song => song.IsFromLibrary = false);
                }

                songs.ForEach(song => song.IsFromLibrary = true);
                CleanOutdatedSongs(previousSongs.Except(songs));

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

                State.Songs = songs;

                MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
                await CacheSongsAsync(cancellationToken);
                if (hasCache && MusicChangeTrackerAvailable && changeReader != null)
                {
                    await changeReader.AcceptChangesAsync();
                }
                else
                {
                    libraryChangeTracker?.Reset();
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsLoadingMusic = false;
                }
            }
        }

        private async Task FetchVideosCancelableAsync(bool useCache, CancellationToken cancellationToken)
        {
            if (VideosLibrary == null) return;
            IsLoadingVideos = true;
            StorageLibraryChangeTracker? libraryChangeTracker = null;
            StorageLibraryChangeReader? changeReader = null;

            try
            {
                useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
                bool hasCache = false;
                await KnownFolders.RequestAccessAsync(KnownFolderId.VideosLibrary);
                StorageFileQueryResult libraryQuery = GetVideosLibraryQuery();
                List<MediaViewModel> videos = new();
                if (useCache)
                {
                    var libraryCache = await LoadStorageLibraryCacheAsync(VideoCacheFileName);
                    if (libraryCache?.Records.Count > 0)
                    {
                        videos = GetMediaFromCache(libraryCache);
                        hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, VideosLibrary);

                        // Update cache with changes from library tracker. Invalidate cache if needed.
                        if (hasCache && VideosChangeTrackerAvailable)
                        {
                            try
                            {
                                libraryChangeTracker = VideosLibrary.ChangeTracker;
                                libraryChangeTracker.Enable();
                                changeReader = libraryChangeTracker.GetChangeReader();
                                hasCache = await TryResolveLibraryChangeAsync(videos, changeReader);
                            }
                            catch (Exception e)
                            {
                                LogService.Log($"Failed to resolve change from library tracker\n{e}");
                            }
                        }
                    }
                }

                // Recrawl the library if there is no cache or cache is invalidated
                if (!hasCache)
                {
                    State.Videos = videos;
                    videos.Clear();
                    await BatchFetchMediaAsync(libraryQuery, videos, cancellationToken);

                    // Search removable storage if the system is Xbox
                    if (SearchRemovableStorage)
                    {
                        var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                        if (accessStatus is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
                        {
                            libraryQuery = CreateRemovableStorageVideosQuery();
                            await BatchFetchMediaAsync(libraryQuery, videos, cancellationToken);
                            StartPortableStorageDeviceWatcher();
                        }
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

                var stateVideos = State.Videos;
                stateVideos.Clear();
                stateVideos.AddRange(videos);

                VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
                await CacheVideosAsync(cancellationToken);
                if (hasCache && VideosChangeTrackerAvailable && changeReader != null)
                {
                    await changeReader.AcceptChangesAsync();
                }
                else
                {
                    libraryChangeTracker?.Reset();
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsLoadingVideos = false;
                }
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
        private void CleanOutdatedSongs(IEnumerable<MediaViewModel> outdatedSongs)
        {
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
            StorageFileQueryResult? libraryQuery = MusicLibraryQueryResult;

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

            MusicLibraryQueryResult = libraryQuery;
            return libraryQuery;
        }

        private StorageFileQueryResult GetVideosLibraryQuery()
        {
            StorageFileQueryResult? libraryQuery = VideosLibraryQueryResult;

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

            VideosLibraryQueryResult = libraryQuery;
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

        private Task<bool> TryResolveLibraryChangeAsync(List<MediaViewModel> mediaList, StorageLibraryChangeReader changeReader)
        {
            if (ApiInformation.IsMethodPresent("Windows.Storage.StorageLibraryChangeReader",
                    "GetLastChangeId"))
            {
                var changeId = changeReader.GetLastChangeId();
                if (changeId == StorageLibraryLastChangeId.Unknown)
                {
                    return Task.FromResult(false);
                }

                if (changeId > 0)
                {
                    return TryResolveLibraryBatchChangeAsync(mediaList, changeReader);
                }
            }
            else
            {
                return TryResolveLibraryBatchChangeAsync(mediaList, changeReader);
            }

            return Task.FromResult(true);
        }

        private async Task<bool> TryResolveLibraryBatchChangeAsync(List<MediaViewModel> mediaList, StorageLibraryChangeReader changeReader)
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
                        file = (StorageFile)await change.GetStorageItemAsync();
                        existing = mediaList.Find(s =>
                            s.Location.Equals(file.Path, StringComparison.OrdinalIgnoreCase));
                        existing?.UpdateSource(file);
                        break;

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
            async void FetchAction()
            {
                try
                {
                    await FetchVideosAsync();
                }
                catch (Exception)
                {
                    // pass   
                }
            }
            // Delay fetch due to query result not yet updated at this time
            _videosRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
        }

        private void OnMusicLibraryContentChanged(object sender, object args)
        {
            async void FetchAction()
            {
                try
                {
                    await FetchMusicAsync();
                }
                catch (Exception)
                {
                    // pass
                }
            }
            ;
            // Delay fetch due to query result not yet updated at this time
            _musicRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
        }

        private void OnPortableStorageDeviceChanged(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (!SearchRemovableStorage) return;
            async void FetchAction()
            {
                try
                {
                    await FetchVideosAsync();
                    await FetchMusicAsync();
                }
                catch (Exception)
                {
                    // pass
                }
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

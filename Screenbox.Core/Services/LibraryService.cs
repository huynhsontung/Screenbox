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
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.System;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service for library management operations
/// </summary>
public sealed class LibraryService : ILibraryService
{
    private bool UseIndexer => _settingsService.UseIndexer;
    private bool SearchRemovableStorage => _settingsService.SearchRemovableStorage && SystemInformation.IsXbox;

    private static readonly string[] CustomPropertyKeys = { SystemProperties.Title };
    private readonly ISettingsService _settingsService;
    private readonly IFilesService _filesService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly DispatcherQueueTimer _musicRefreshTimer;
    private readonly DispatcherQueueTimer _videosRefreshTimer;
    private readonly DispatcherQueueTimer _storageDeviceRefreshTimer;
    private readonly DeviceWatcher? _portableStorageDeviceWatcher;

    private const string SongsCacheFileName = "songs.bin";
    private const string VideoCacheFileName = "videos.bin";

    public LibraryService(ISettingsService settingsService, IFilesService filesService,
        MediaViewModelFactory mediaFactory)
    {
        _settingsService = settingsService;
        _filesService = filesService;
        _mediaFactory = mediaFactory;
        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _musicRefreshTimer = dispatcherQueue.CreateTimer();
        _videosRefreshTimer = dispatcherQueue.CreateTimer();
        _storageDeviceRefreshTimer = dispatcherQueue.CreateTimer();

        if (SystemInformation.IsXbox)
        {
            _portableStorageDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
            _portableStorageDeviceWatcher.Removed += (sender, args) => OnPortableStorageDeviceChanged(sender, args, null);
            _portableStorageDeviceWatcher.Updated += (sender, args) => OnPortableStorageDeviceChanged(sender, args, null);
        }
    }

    public async Task<StorageLibrary> InitializeMusicLibraryAsync(LibraryContext context)
    {
        // No need to add handler for StorageLibrary.DefinitionChanged
        context.MusicLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
        try
        {
            context.MusicLibrary.ChangeTracker.Enable();
            context.MusicChangeTrackerAvailable = true;
        }
        catch (Exception)
        {
            // pass
        }

        return context.MusicLibrary;
    }

    public async Task<StorageLibrary> InitializeVideosLibraryAsync(LibraryContext context)
    {
        // No need to add handler for StorageLibrary.DefinitionChanged
        context.VideosLibrary ??= await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
        try
        {
            context.VideosLibrary.ChangeTracker.Enable();
            context.VideosChangeTrackerAvailable = true;
        }
        catch (Exception)
        {
            // pass
        }

        return context.VideosLibrary;
    }

    public async Task FetchMusicAsync(LibraryContext context, bool useCache = true)
    {
        context.MusicFetchCts?.Cancel();
        using CancellationTokenSource cts = new();
        context.MusicFetchCts = cts;
        try
        {
            await InitializeMusicLibraryAsync(context);
            cts.Token.ThrowIfCancellationRequested();
            await FetchMusicCancelableAsync(context, useCache, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        finally
        {
            context.MusicFetchCts = null;
        }
    }

    public async Task FetchVideosAsync(LibraryContext context, bool useCache = true)
    {
        context.VideosFetchCts?.Cancel();
        using CancellationTokenSource cts = new();
        context.VideosFetchCts = cts;
        try
        {
            await InitializeVideosLibraryAsync(context);
            cts.Token.ThrowIfCancellationRequested();
            await FetchVideosCancelableAsync(context, useCache, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        finally
        {
            context.VideosFetchCts = null;
        }
    }

    public void RemoveMedia(LibraryContext context, MediaViewModel media)
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
        var songs = context.Songs.ToList();
        songs.Remove(media);
        context.Songs = songs;
        var videos = context.Videos.ToList();
        videos.Remove(media);
        context.Videos = videos;
    }

    private async Task CacheSongsAsync(LibraryContext context, CancellationToken cancellationToken)
    {
        var folderPaths = context.MusicLibrary!.Folders.Select(f => f.Path).ToList();
        var records = context.Songs.Select(song =>
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

    private async Task CacheVideosAsync(LibraryContext context, CancellationToken cancellationToken)
    {
        var folderPaths = context.VideosLibrary!.Folders.Select(f => f.Path).ToList();
        List<PersistentMediaRecord> records = context.Videos.Select(video =>
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

    private async Task FetchMusicCancelableAsync(LibraryContext context, bool useCache, CancellationToken cancellationToken)
    {
        if (context.MusicLibrary == null) return;
        context.IsLoadingMusic = true;
        StorageLibraryChangeTracker? libraryChangeTracker = null;
        StorageLibraryChangeReader? changeReader = null;
        try
        {
            useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
            bool hasCache = false;
            await KnownFolders.RequestAccessAsync(KnownFolderId.MusicLibrary);
            var libraryQuery = GetMusicLibraryQuery(context);
            List<MediaViewModel> songs = new();
            if (useCache)
            {
                var libraryCache = await LoadStorageLibraryCacheAsync(SongsCacheFileName);
                if (libraryCache?.Records.Count > 0)
                {
                    songs = GetMediaFromCache(libraryCache);
                    hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, context.MusicLibrary);

                    // Update cache with changes from library tracker. Invalidate cache if needed.
                    if (hasCache && context.MusicChangeTrackerAvailable)
                    {
                        try
                        {
                            libraryChangeTracker = context.MusicLibrary.ChangeTracker;
                            libraryChangeTracker.Enable();
                            changeReader = libraryChangeTracker.GetChangeReader();
                            hasCache = await TryResolveLibraryChangeAsync(context, songs, changeReader);
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
                songs = new List<MediaViewModel>();
                context.Songs = songs;
                await BatchFetchMediaAsync(libraryQuery, songs, cancellationToken);

                // Search removable storage if the system is Xbox
                if (SearchRemovableStorage)
                {
                    var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                    if (accessStatus is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
                    {
                        libraryQuery = CreateRemovableStorageMusicQuery();
                        await BatchFetchMediaAsync(libraryQuery, songs, cancellationToken);
                        StartPortableStorageDeviceWatcher(context);
                    }
                }
            }

            // After async operation we need to check the context still have the same reference
            if (songs != context.Songs)
            {
                // Ensure only songs not in the library has IsFromLibrary = false
                foreach (var song in context.Songs)
                {
                    song.IsFromLibrary = false;
                }
            }

            songs.ForEach(song => song.IsFromLibrary = true);
            CleanOutdatedSongs(context);

            // Populate Album and Artists for each song
            var albumFactory = new AlbumViewModelFactory();
            var artistFactory = new ArtistViewModelFactory();
            foreach (MediaViewModel song in songs)
            {
                if (!song.IsFromLibrary) continue;
                // A cached song always has a URI as source
                if (hasCache && song.Source is Uri)
                {
                    albumFactory.AddSong(song);
                    artistFactory.AddSong(song);
                }
                else
                {
                    await song.LoadDetailsAsync(_filesService);
                    cancellationToken.ThrowIfCancellationRequested();
                    albumFactory.AddSong(song);
                    artistFactory.AddSong(song);
                }
            }

            UpdateMusicLibraryContext(context, songs, albumFactory, artistFactory);
            await CacheSongsAsync(context, cancellationToken);
            if (hasCache && context.MusicChangeTrackerAvailable && changeReader != null)
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
                context.IsLoadingMusic = false;
            }
        }
    }

    private static void UpdateMusicLibraryContext(LibraryContext context, List<MediaViewModel> songs,
        AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
    {
        context.Songs = songs;
        context.Albums = albumFactory.Albums;
        context.UnknownAlbum = albumFactory.UnknownAlbum;
        foreach (var (song, album) in albumFactory.SongsToAlbums)
        {
            song.Album = album;
        }

        context.Artists = artistFactory.Artists;
        context.UnknownArtist = artistFactory.UnknownArtist;
        foreach (var (song, artists) in artistFactory.SongsToArtists)
        {
            song.Artists = artists.ToArray();
        }

        context.RaiseMusicLibraryContentChanged();
    }

    private async Task FetchVideosCancelableAsync(LibraryContext context, bool useCache, CancellationToken cancellationToken)
    {
        if (context.VideosLibrary == null) return;
        context.IsLoadingVideos = true;
        StorageLibraryChangeTracker? libraryChangeTracker = null;
        StorageLibraryChangeReader? changeReader = null;

        try
        {
            useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
            bool hasCache = false;
            await KnownFolders.RequestAccessAsync(KnownFolderId.VideosLibrary);
            StorageFileQueryResult libraryQuery = GetVideosLibraryQuery(context);
            List<MediaViewModel> videos = new();
            if (useCache)
            {
                var libraryCache = await LoadStorageLibraryCacheAsync(VideoCacheFileName);
                if (libraryCache?.Records.Count > 0)
                {
                    videos = GetMediaFromCache(libraryCache);
                    hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, context.VideosLibrary);

                    // Update cache with changes from library tracker. Invalidate cache if needed.
                    if (hasCache && context.VideosChangeTrackerAvailable)
                    {
                        try
                        {
                            libraryChangeTracker = context.VideosLibrary.ChangeTracker;
                            libraryChangeTracker.Enable();
                            changeReader = libraryChangeTracker.GetChangeReader();
                            hasCache = await TryResolveLibraryChangeAsync(context, videos, changeReader);
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
                context.Videos = videos;
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
                        StartPortableStorageDeviceWatcher(context);
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

            context.Videos = videos;
            context.RaiseVideosLibraryContentChanged();
            await CacheVideosAsync(context, cancellationToken);
            if (hasCache && context.VideosChangeTrackerAvailable && changeReader != null)
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
                context.IsLoadingVideos = false;
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
    private void CleanOutdatedSongs(LibraryContext context)
    {
        List<MediaViewModel> outdatedSongs = context.Songs.Where(song => !song.IsFromLibrary).ToList();
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
    }

    private StorageFileQueryResult GetMusicLibraryQuery(LibraryContext context)
    {
        StorageFileQueryResult? libraryQuery = context.MusicLibraryQueryResult;

        if (libraryQuery != null && ShouldUpdateQuery(libraryQuery, UseIndexer))
        {
            libraryQuery.ContentsChanged -= (sender, args) => OnMusicLibraryContentChanged(context, sender, args);
            libraryQuery = null;
        }

        if (libraryQuery == null)
        {
            libraryQuery = CreateMusicLibraryQuery(UseIndexer);
            libraryQuery.ContentsChanged += (sender, args) => OnMusicLibraryContentChanged(context, sender, args);
        }

        context.MusicLibraryQueryResult = libraryQuery;
        return libraryQuery;
    }

    private StorageFileQueryResult GetVideosLibraryQuery(LibraryContext context)
    {
        StorageFileQueryResult? libraryQuery = context.VideosLibraryQueryResult;

        if (libraryQuery != null && ShouldUpdateQuery(libraryQuery, UseIndexer))
        {
            libraryQuery.ContentsChanged -= (sender, args) => OnVideosLibraryContentChanged(context, sender, args);
            libraryQuery = null;
        }

        if (libraryQuery == null)
        {
            libraryQuery = CreateVideosLibraryQuery(UseIndexer);
            libraryQuery.ContentsChanged += (sender, args) => OnVideosLibraryContentChanged(context, sender, args);
        }

        context.VideosLibraryQueryResult = libraryQuery;
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

    private Task<bool> TryResolveLibraryChangeAsync(LibraryContext context, List<MediaViewModel> mediaList, StorageLibraryChangeReader changeReader)
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
                return TryResolveLibraryBatchChangeAsync(context, mediaList, changeReader);
            }
        }
        else
        {
            return TryResolveLibraryBatchChangeAsync(context, mediaList, changeReader);
        }

        return Task.FromResult(true);
    }

    private async Task<bool> TryResolveLibraryBatchChangeAsync(LibraryContext context, List<MediaViewModel> mediaList, StorageLibraryChangeReader changeReader)
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
                        RemoveMedia(context, existing);
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
                        RemoveMedia(context, existing);
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

    private void OnVideosLibraryContentChanged(LibraryContext context, object sender, object args)
    {
        async void FetchAction()
        {
            try
            {
                await FetchVideosAsync(context);
            }
            catch (Exception)
            {
                // pass   
            }
        }
        // Delay fetch due to query result not yet updated at this time
        _videosRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
    }

    private void OnMusicLibraryContentChanged(LibraryContext context, object sender, object args)
    {
        async void FetchAction()
        {
            try
            {
                await FetchMusicAsync(context);
            }
            catch (Exception)
            {
                // pass
            }
        }

        // Delay fetch due to query result not yet updated at this time
        _musicRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
    }

    private void OnPortableStorageDeviceChanged(DeviceWatcher sender, DeviceInformationUpdate args, LibraryContext? context)
    {
        if (!SearchRemovableStorage || context == null) return;
        async void FetchAction()
        {
            try
            {
                await FetchVideosAsync(context);
                await FetchMusicAsync(context);
            }
            catch (Exception)
            {
                // pass
            }
        }
        _storageDeviceRefreshTimer.Debounce(FetchAction, TimeSpan.FromMilliseconds(1000));
    }

    private void StartPortableStorageDeviceWatcher(LibraryContext context)
    {
        if (_portableStorageDeviceWatcher?.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped)
        {
            // Update event handlers to use the context
            _portableStorageDeviceWatcher.Removed += (sender, args) => OnPortableStorageDeviceChanged(sender, args, context);
            _portableStorageDeviceWatcher.Updated += (sender, args) => OnPortableStorageDeviceChanged(sender, args, context);
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

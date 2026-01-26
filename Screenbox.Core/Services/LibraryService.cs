#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service for library management operations
/// </summary>
public sealed class LibraryService : ILibraryService
{
    private bool SearchRemovableStorage => _settingsService.SearchRemovableStorage && SystemInformation.IsXbox;

    private static readonly string[] CustomPropertyKeys = { SystemProperties.Title };
    private readonly ISettingsService _settingsService;
    private readonly IFilesService _filesService;
    private readonly MediaViewModelFactory _mediaFactory;

    private const string SongsCacheFileName = "songs.bin";
    private const string VideoCacheFileName = "videos.bin";

    public LibraryService(ISettingsService settingsService, IFilesService filesService,
        MediaViewModelFactory mediaFactory)
    {
        _settingsService = settingsService;
        _filesService = filesService;
        _mediaFactory = mediaFactory;
    }

    public StorageFileQueryResult CreateMusicLibraryQuery(bool useIndexer)
    {
        // Uses the same query options as the service's fetch logic. Centralized to avoid duplication.
        QueryOptions queryOptions = new(CommonFileQuery.OrderByTitle, FilesHelpers.SupportedAudioFormats)
        {
            IndexerOption = useIndexer ? IndexerOption.UseIndexerWhenAvailable : IndexerOption.DoNotUseIndexer
        };
        queryOptions.SetPropertyPrefetch(
            PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.MusicProperties,
            CustomPropertyKeys);

        return KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOptions);
    }

    public StorageFileQueryResult CreateVideosLibraryQuery(bool useIndexer)
    {
        // Uses the same query options as the service's fetch logic. Centralized to avoid duplication.
        QueryOptions queryOptions = new(CommonFileQuery.OrderByName, FilesHelpers.SupportedVideoFormats)
        {
            IndexerOption = useIndexer ? IndexerOption.UseIndexerWhenAvailable : IndexerOption.DoNotUseIndexer
        };
        queryOptions.SetPropertyPrefetch(
            PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.VideoProperties,
            CustomPropertyKeys);

        return KnownFolders.VideosLibrary.CreateFileQueryWithOptions(queryOptions);
    }

    public async Task<StorageLibrary> InitializeMusicLibraryAsync()
    {
        // No need to add handler for StorageLibrary.DefinitionChanged
        var library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
        try
        {
            library.ChangeTracker.Enable();
        }
        catch (Exception)
        {
            // pass
        }

        return library;
    }

    public async Task<StorageLibrary> InitializeVideosLibraryAsync()
    {
        // No need to add handler for StorageLibrary.DefinitionChanged
        var library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
        try
        {
            library.ChangeTracker.Enable();
        }
        catch (Exception)
        {
            // pass
        }

        return library;
    }

    public async Task FetchMusicAsync(LibraryContext context, bool useCache = true, IProgress<List<MediaViewModel>>? progress = default)
    {
        context.MusicFetchCts?.Cancel();
        using CancellationTokenSource cts = new();
        context.MusicFetchCts = cts;
        try
        {
            await FetchMusicCancelableAsync(context, useCache, progress ?? new Progress<List<MediaViewModel>>(), cts.Token);
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

    public async Task FetchVideosAsync(LibraryContext context, bool useCache = true, IProgress<List<MediaViewModel>>? progress = default)
    {
        context.VideosFetchCts?.Cancel();
        using CancellationTokenSource cts = new();
        context.VideosFetchCts = cts;
        try
        {
            await FetchVideosCancelableAsync(context, useCache, progress ?? new Progress<List<MediaViewModel>>(), cts.Token);
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
        // TODO: Consider treating library objects as immutable and create a new list instead
        // to avoid exception when the collection is being enumerated
        context.MusicLibrary.Songs.Remove(media);
        context.VideosLibrary.Videos.Remove(media);
    }

    private async Task CacheSongsAsync(LibraryContext context, CancellationToken cancellationToken)
    {
        var folderPaths = context.StorageMusicLibrary!.Folders.Select(f => f.Path).ToList();
        var records = context.MusicLibrary.Songs.Select(song =>
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
        var folderPaths = context.StorageVideosLibrary!.Folders.Select(f => f.Path).ToList();
        List<PersistentMediaRecord> records = context.VideosLibrary.Videos.Select(video =>
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

    private async Task FetchMusicCancelableAsync(LibraryContext context, bool useCache,
        IProgress<List<MediaViewModel>> progress, CancellationToken cancellationToken)
    {
        if (context.StorageMusicLibrary == null || context.MusicLibraryQueryResult == null) return;
        context.IsLoadingMusic = true;
        var existingLibrary = context.MusicLibrary;
        StorageFileQueryResult libraryQuery = context.MusicLibraryQueryResult;
        StorageLibraryChangeTracker? libraryChangeTracker = null;
        StorageLibraryChangeReader? changeReader = null;
        try
        {
            useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
            bool hasCache = false;

            List<MediaViewModel> songs = new();
            if (useCache)
            {
                var libraryCache = await LoadStorageLibraryCacheAsync(SongsCacheFileName);
                if (libraryCache?.Records.Count > 0)
                {
                    songs = GetMediaFromCache(libraryCache);
                    hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, context.StorageMusicLibrary);

                    // Update cache with changes from library tracker. Invalidate cache if needed.
                    if (hasCache)
                    {
                        try
                        {
                            libraryChangeTracker = context.StorageMusicLibrary.ChangeTracker;
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
                List<StorageFileQueryResult> queries = [libraryQuery];
                if (SearchRemovableStorage)
                {
                    var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                    if (accessStatus is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
                    {
                        queries.Add(CreateRemovableStorageMusicQuery());
                    }
                }

                songs = new List<MediaViewModel>();
                foreach (var query in queries)
                {
                    await BatchFetchMediaAsync(query, songs, progress, cancellationToken);
                }
            }

            // After async operation we need to check the context still have the same reference
            if (existingLibrary == context.MusicLibrary)
            {
                // Ensure only songs not in the library has IsFromLibrary = false
                foreach (var song in existingLibrary.Songs)
                {
                    song.IsFromLibrary = false;
                }
            }

            songs.ForEach(song => song.IsFromLibrary = true);
            CleanOutdatedSongs(existingLibrary);

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
            if (hasCache && changeReader != null)
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
        foreach (var (song, album) in albumFactory.SongsToAlbums)
        {
            song.Album = album;
        }

        foreach (var (song, artists) in artistFactory.SongsToArtists)
        {
            song.Artists = artists.ToArray();
        }

        context.MusicLibrary = new MusicLibrary(songs, albumFactory.Albums, artistFactory.Artists,
            albumFactory.UnknownAlbum, artistFactory.UnknownArtist);
    }

    private async Task FetchVideosCancelableAsync(LibraryContext context, bool useCache,
        IProgress<List<MediaViewModel>> progress, CancellationToken cancellationToken)
    {
        if (context.StorageVideosLibrary == null || context.VideosLibraryQueryResult == null) return;
        var existingLibrary = context.VideosLibrary;
        StorageFileQueryResult libraryQuery = context.VideosLibraryQueryResult;
        context.IsLoadingVideos = true;
        StorageLibraryChangeTracker? libraryChangeTracker = null;
        StorageLibraryChangeReader? changeReader = null;

        try
        {
            useCache = useCache && !SystemInformation.IsXbox;   // Don't use cache on Xbox
            bool hasCache = false;

            List<MediaViewModel> videos = new();
            if (useCache)
            {
                var libraryCache = await LoadStorageLibraryCacheAsync(VideoCacheFileName);
                if (libraryCache?.Records.Count > 0)
                {
                    videos = GetMediaFromCache(libraryCache);
                    hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, context.StorageVideosLibrary);

                    // Update cache with changes from library tracker. Invalidate cache if needed.
                    if (hasCache)
                    {
                        try
                        {
                            libraryChangeTracker = context.StorageVideosLibrary.ChangeTracker;
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
                List<StorageFileQueryResult> queries = [libraryQuery];
                if (SearchRemovableStorage)
                {
                    var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                    if (accessStatus is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
                    {
                        queries.Add(CreateRemovableStorageVideosQuery());
                    }
                }

                videos = new List<MediaViewModel>();
                foreach (var query in queries)
                {
                    await BatchFetchMediaAsync(query, videos, progress, cancellationToken);
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

            context.VideosLibrary = new VideosLibrary(videos);
            await CacheVideosAsync(context, cancellationToken);
            if (hasCache && changeReader != null)
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

    private async Task BatchFetchMediaAsync(StorageFileQueryResult queryResult, List<MediaViewModel> target,
        IProgress<List<MediaViewModel>> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lastProgressReport = DateTimeOffset.Now;
        while (true)
        {
            List<MediaViewModel> batch = await FetchMediaFromStorage(queryResult, (uint)target.Count);
            if (batch.Count == 0) break;
            target.AddRange(batch);
            cancellationToken.ThrowIfCancellationRequested();
            if ((DateTimeOffset.Now - lastProgressReport).TotalSeconds > 3)
            {
                // Report progress if the operation takes long enough
                progress.Report([.. target]);
                lastProgressReport = DateTimeOffset.Now;
            }
        }
    }

    /// <summary>
    /// Clean up songs that are no longer from the library
    /// </summary>
    private void CleanOutdatedSongs(MusicLibrary library)
    {
        List<MediaViewModel> outdatedSongs = library.Songs.Where(song => !song.IsFromLibrary).ToList();
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

    private static bool AreLibraryPathsChanged(IReadOnlyCollection<string> cachedFolderPaths, StorageLibrary library)
    {
        var paths = library.Folders.Select(f => f.Path).ToList();
        if (cachedFolderPaths.Count != paths.Count) return true;
        return cachedFolderPaths.Any(cachedPath => !paths.Contains(cachedPath, StringComparer.OrdinalIgnoreCase));
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

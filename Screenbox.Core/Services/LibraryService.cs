#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public async Task<MusicLibrary> FetchMusicAsync(
        StorageLibrary library,
        StorageFileQueryResult queryResult,
        bool useCache,
        CancellationToken cancellationToken,
        IProgress<MusicLibrary>? progress = null)
    {
        StorageFileQueryResult libraryQuery = queryResult;
        StorageLibraryChangeTracker? libraryChangeTracker = null;
        StorageLibraryChangeReader? changeReader = null;

        useCache = useCache && !SystemInformation.IsXbox;
        bool hasCache = false;

        List<MediaViewModel> songs = new();
        if (useCache)
        {
            var libraryCache = await LoadStorageLibraryCacheAsync(SongsCacheFileName);
            if (libraryCache?.Records.Count > 0)
            {
                songs = GetMediaFromCache(libraryCache);
                hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, library);

                if (hasCache)
                {
                    try
                    {
                        libraryChangeTracker = library.ChangeTracker;
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

        var albumFactory = new AlbumViewModelFactory();
        var artistFactory = new ArtistViewModelFactory();

        if (!hasCache)
        {
            songs = new List<MediaViewModel>();
            await BatchFetchMusicAsync(libraryQuery, songs, albumFactory, artistFactory, cancellationToken, progress);

            if (SearchRemovableStorage)
            {
                var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                if (accessStatus is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
                {
                    libraryQuery = CreateRemovableStorageMusicQuery();
                    await BatchFetchMusicAsync(libraryQuery, songs, albumFactory, artistFactory, cancellationToken, progress);
                }
            }
        }
        else
        {
            // Cache path: build associations in one pass (fast, no async detail loading needed)
            foreach (MediaViewModel song in songs)
            {
                song.IsFromLibrary = true;
                albumFactory.AddSong(song);
                artistFactory.AddSong(song);
                song.Album = albumFactory.SongsToAlbums[song];
                song.Artists = artistFactory.SongsToArtists[song].ToArray();
            }
        }

        var result = new MusicLibrary(
            songs,
            albumFactory.Albums,
            artistFactory.Artists,
            albumFactory.UnknownAlbum,
            artistFactory.UnknownArtist);

        await CacheSongsAsync(library, songs, cancellationToken);
        if (hasCache && changeReader != null)
        {
            await changeReader.AcceptChangesAsync();
        }
        else
        {
            libraryChangeTracker?.Reset();
        }

        return result;
    }

    public async Task<VideosLibrary> FetchVideosAsync(
        StorageLibrary library,
        StorageFileQueryResult queryResult,
        bool useCache,
        CancellationToken cancellationToken,
        IProgress<VideosLibrary>? progress = null)
    {
        StorageFileQueryResult libraryQuery = queryResult;
        StorageLibraryChangeTracker? libraryChangeTracker = null;
        StorageLibraryChangeReader? changeReader = null;

        useCache = useCache && !SystemInformation.IsXbox;
        bool hasCache = false;

        List<MediaViewModel> videos = new();
        if (useCache)
        {
            var libraryCache = await LoadStorageLibraryCacheAsync(VideoCacheFileName);
            if (libraryCache?.Records.Count > 0)
            {
                videos = GetMediaFromCache(libraryCache);
                hasCache = !AreLibraryPathsChanged(libraryCache.FolderPaths, library);

                if (hasCache)
                {
                    try
                    {
                        libraryChangeTracker = library.ChangeTracker;
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

        if (!hasCache)
        {
            videos = new List<MediaViewModel>();
            await BatchFetchVideosAsync(libraryQuery, videos, cancellationToken, progress);

            if (SearchRemovableStorage)
            {
                var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                if (accessStatus is KnownFoldersAccessStatus.Allowed or KnownFoldersAccessStatus.AllowedPerAppFolder)
                {
                    libraryQuery = CreateRemovableStorageVideosQuery();
                    await BatchFetchVideosAsync(libraryQuery, videos, cancellationToken, progress);
                }
            }
        }
        else
        {
            foreach (MediaViewModel video in videos)
            {
                video.IsFromLibrary = true;
            }
        }

        await CacheVideosAsync(library, videos, cancellationToken);
        if (hasCache && changeReader != null)
        {
            await changeReader.AcceptChangesAsync();
        }
        else
        {
            libraryChangeTracker?.Reset();
        }

        return new VideosLibrary(videos);
    }

    private static void DetachMediaRelationships(MediaViewModel media)
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
    }

    private async Task CacheSongsAsync(StorageLibrary library, List<MediaViewModel> songs, CancellationToken cancellationToken)
    {
        var folderPaths = library.Folders.Select(f => f.Path).ToList();
        var records = songs.Select(song =>
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

    private async Task CacheVideosAsync(StorageLibrary library, List<MediaViewModel> videos, CancellationToken cancellationToken)
    {
        var folderPaths = library.Folders.Select(f => f.Path).ToList();
        List<PersistentMediaRecord> records = videos.Select(video =>
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
            MediaViewModel media = _mediaFactory.GetOrCreate(new Uri(record.Path));
            media.IsFromLibrary = true;
            if (!media.DetailsLoaded)
            {
                if (!string.IsNullOrEmpty(record.Title))
                    media.Name = record.Title;
                media.MediaInfo = record.Properties != null
                    ? new MediaInfo(record.Properties)
                    : new MediaInfo(record.MediaType, record.Title, record.Year, record.Duration);
            }

            if (record.DateAdded != default)
            {
                DateTimeOffset utcTime = DateTime.SpecifyKind(record.DateAdded, DateTimeKind.Utc);
                media.DateAdded = utcTime.ToLocalTime();
            }

            return media;
        }).ToList();
        return mediaList;
    }

    private async Task BatchFetchMusicAsync(
        StorageFileQueryResult queryResult,
        List<MediaViewModel> target,
        AlbumViewModelFactory albumFactory,
        ArtistViewModelFactory artistFactory,
        CancellationToken cancellationToken,
        IProgress<MusicLibrary>? progress)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime lastReport = DateTime.MinValue;
        while (true)
        {
            IReadOnlyList<StorageFile> files = await FetchFilesFromStorage(queryResult, (uint)target.Count);
            if (files.Count == 0) break;

            foreach (StorageFile file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MediaViewModel song = _mediaFactory.Create(file);
                song.IsFromLibrary = true;
                await song.LoadDetailsAsync(_filesService);
                albumFactory.AddSong(song);
                artistFactory.AddSong(song);
                song.Album = albumFactory.SongsToAlbums[song];
                song.Artists = artistFactory.SongsToArtists[song].ToArray();
                target.Add(song);
            }

            if (progress != null && (DateTime.UtcNow - lastReport).TotalSeconds >= 5)
            {
                progress.Report(BuildMusicSnapshot(target, albumFactory, artistFactory));
                lastReport = DateTime.UtcNow;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task BatchFetchVideosAsync(
        StorageFileQueryResult queryResult,
        List<MediaViewModel> target,
        CancellationToken cancellationToken,
        IProgress<VideosLibrary>? progress)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime lastReport = DateTime.MinValue;
        while (true)
        {
            IReadOnlyList<StorageFile> files = await FetchFilesFromStorage(queryResult, (uint)target.Count);
            if (files.Count == 0) break;

            foreach (StorageFile file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MediaViewModel video = _mediaFactory.Create(file);
                video.IsFromLibrary = true;
                await video.LoadDetailsAsync(_filesService);
                target.Add(video);
            }

            if (progress != null && (DateTime.UtcNow - lastReport).TotalSeconds >= 5)
            {
                progress.Report(new VideosLibrary(new List<MediaViewModel>(target)));
                lastReport = DateTime.UtcNow;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static MusicLibrary BuildMusicSnapshot(
        List<MediaViewModel> songs,
        AlbumViewModelFactory albumFactory,
        ArtistViewModelFactory artistFactory)
    {
        return new MusicLibrary(
            new List<MediaViewModel>(songs),
            new Dictionary<string, AlbumViewModel>(albumFactory.Albums),
            new Dictionary<string, ArtistViewModel>(artistFactory.Artists),
            albumFactory.UnknownAlbum,
            artistFactory.UnknownArtist);
    }

    private async Task<IReadOnlyList<StorageFile>> FetchFilesFromStorage(StorageFileQueryResult queryResult, uint fetchIndex, uint batchSize = 50)
    {
        try
        {
            return await queryResult.GetFilesAsync(fetchIndex, batchSize);
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

            return Array.Empty<StorageFile>();
        }
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
                    mediaList.Add(_mediaFactory.GetOrCreate(file));
                    break;

                case StorageLibraryChangeType.Deleted:
                case StorageLibraryChangeType.MovedOutOfLibrary:
                    existing = mediaList.Find(s =>
                        s.Location.Equals(change.PreviousPath, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        DetachMediaRelationships(existing);
                        mediaList.Remove(existing);
                    }

                    break;

                case StorageLibraryChangeType.MovedOrRenamed:
                    file = (StorageFile)await change.GetStorageItemAsync();
                    existing = mediaList.Find(s =>
                        s.Location.Equals(change.PreviousPath, StringComparison.OrdinalIgnoreCase));

                    var newMedia = _mediaFactory.GetOrCreate(file);
                    if (existing != null)
                    {
                        var existingInfo = existing.MediaInfo;
                        DetachMediaRelationships(existing);
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

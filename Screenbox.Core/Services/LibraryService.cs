#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Data;
using Screenbox.Core.Enums;
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
    private readonly IScreenboxDatabase _database;

    public LibraryService(ISettingsService settingsService, IFilesService filesService,
        MediaViewModelFactory mediaFactory, IScreenboxDatabase database)
    {
        _settingsService = settingsService;
        _filesService = filesService;
        _mediaFactory = mediaFactory;
        _database = database;
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

    public async Task FetchMusicAsync(LibraryContext context, bool useCache = true)
    {
        context.MusicFetchCts?.Cancel();
        using CancellationTokenSource cts = new();
        context.MusicFetchCts = cts;
        try
        {
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
        var records = context.Songs.Select(song => CreateMediaRecord(song, MediaPlaybackType.Music)).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _database.SaveLibraryCacheAsync(MediaPlaybackType.Music, records, folderPaths);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async Task CacheVideosAsync(LibraryContext context, CancellationToken cancellationToken)
    {
        var folderPaths = context.VideosLibrary!.Folders.Select(f => f.Path).ToList();
        var records = context.Videos.Select(video => CreateMediaRecord(video, MediaPlaybackType.Video)).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _database.SaveLibraryCacheAsync(MediaPlaybackType.Video, records, folderPaths);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static MediaRecordEntity CreateMediaRecord(MediaViewModel media, MediaPlaybackType libraryType)
    {
        var record = new MediaRecordEntity
        {
            LibraryType = libraryType,
            Title = media.Name,
            Path = media.Location,
            MediaType = media.MediaInfo.MediaType,
            DateAddedUtc = media.DateAdded.UtcDateTime,
        };

        if (media.MediaInfo.MediaType == MediaPlaybackType.Music)
        {
            MusicInfo music = media.MediaInfo.MusicProperties;
            record.DurationTicks = music.Duration.Ticks;
            record.Year = music.Year;
            record.Artist = music.Artist;
            record.Album = music.Album;
            record.AlbumArtist = music.AlbumArtist;
            record.Composers = music.Composers;
            record.Genre = music.Genre;
            record.TrackNumber = music.TrackNumber;
            record.MusicBitrate = music.Bitrate;
        }
        else
        {
            VideoInfo video = media.MediaInfo.VideoProperties;
            record.DurationTicks = video.Duration.Ticks;
            record.Year = video.Year;
            record.VideoSubtitle = video.Subtitle;
            record.Producers = video.Producers;
            record.Writers = video.Writers;
            record.Width = video.Width;
            record.Height = video.Height;
            record.VideoBitrate = video.Bitrate;
        }

        return record;
    }

    private async Task<(List<MediaRecordEntity>? Records, List<string>? FolderPaths)> LoadLibraryCacheAsync(
        MediaPlaybackType libraryType)
    {
        try
        {
            var result = await _database.LoadLibraryCacheAsync(libraryType);
            return (result.Records, result.FolderPaths);
        }
        catch (Exception)
        {
            // DB not ready or corrupt — triggers a recrawl
            return (null, null);
        }
    }

    private List<MediaViewModel> GetMediaFromCache(List<MediaRecordEntity> records)
    {
        return records.Select(record =>
        {
            MediaViewModel media = _mediaFactory.GetSingleton(new Uri(record.Path));
            media.IsFromLibrary = true;
            if (!media.DetailsLoaded)
            {
                if (!string.IsNullOrEmpty(record.Title))
                    media.Name = record.Title;

                if (record.MediaType == MediaPlaybackType.Music)
                {
                    var musicInfo = new MusicInfo
                    {
                        Title = record.Title,
                        Artist = record.Artist ?? string.Empty,
                        Album = record.Album ?? string.Empty,
                        AlbumArtist = record.AlbumArtist ?? string.Empty,
                        Composers = record.Composers ?? string.Empty,
                        Genre = record.Genre ?? string.Empty,
                        TrackNumber = record.TrackNumber ?? 0,
                        Year = record.Year,
                        Duration = TimeSpan.FromTicks(record.DurationTicks),
                        Bitrate = record.MusicBitrate ?? 0
                    };
                    media.MediaInfo = new MediaInfo(musicInfo);
                }
                else
                {
                    var videoInfo = new VideoInfo
                    {
                        Title = record.Title,
                        Subtitle = record.VideoSubtitle ?? string.Empty,
                        Producers = record.Producers ?? string.Empty,
                        Writers = record.Writers ?? string.Empty,
                        Year = record.Year,
                        Duration = TimeSpan.FromTicks(record.DurationTicks),
                        Width = record.Width ?? 0,
                        Height = record.Height ?? 0,
                        Bitrate = record.VideoBitrate ?? 0
                    };
                    media.MediaInfo = new MediaInfo(videoInfo);
                }
            }

            if (record.DateAddedUtc != default)
            {
                media.DateAdded = DateTime.SpecifyKind(record.DateAddedUtc, DateTimeKind.Utc).ToLocalTime();
            }

            return media;
        }).ToList();
    }

    private async Task FetchMusicCancelableAsync(LibraryContext context, bool useCache, CancellationToken cancellationToken)
    {
        if (context.MusicLibrary == null || context.MusicLibraryQueryResult == null) return;
        context.IsLoadingMusic = true;
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
                var (cachedRecords, cachedFolderPaths) = await LoadLibraryCacheAsync(MediaPlaybackType.Music);
                if (cachedRecords?.Count > 0 && cachedFolderPaths != null)
                {
                    songs = GetMediaFromCache(cachedRecords);
                    hasCache = !AreLibraryPathsChanged(cachedFolderPaths, context.MusicLibrary);

                    // Update cache with changes from library tracker. Invalidate cache if needed.
                    if (hasCache)
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
        if (context.VideosLibrary == null || context.VideosLibraryQueryResult == null) return;
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
                var (cachedRecords, cachedFolderPaths) = await LoadLibraryCacheAsync(MediaPlaybackType.Video);
                if (cachedRecords?.Count > 0 && cachedFolderPaths != null)
                {
                    videos = GetMediaFromCache(cachedRecords);
                    hasCache = !AreLibraryPathsChanged(cachedFolderPaths, context.VideosLibrary);

                    // Update cache with changes from library tracker. Invalidate cache if needed.
                    if (hasCache)
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

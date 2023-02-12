#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Screenbox.Core.Database;
using Screenbox.Factories;
using Screenbox.ViewModels;
using Windows.Storage;
using Windows.Storage.Search;
using ProtoBuf;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal sealed class LibraryService : ILibraryService
    {
        public event TypedEventHandler<ILibraryService, object>? MusicLibraryContentChanged; 
        public event TypedEventHandler<ILibraryService, object>? VideosLibraryContentChanged;

        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly object _lockObject;

        private const int MaxLoadCount = 5000;
        private const string MusicSaveFileName = "music_library.bin";
        private const string VideosSaveFileName = "videos_library.bin";

        private Task<MusicLibraryFetchResult>? _loadMusicTask;
        private Task<IReadOnlyList<MediaViewModel>>? _loadVideosTask;
        private List<MediaViewModel> _songs;
        private List<AlbumViewModel> _albums;
        private List<ArtistViewModel> _artists;
        private List<MediaViewModel> _videos;
        private StorageLibrary? _musicLibrary;
        private StorageLibrary? _videosLibrary;
        private StorageFileQueryResult? _musicLibraryQueryResult;
        private StorageFileQueryResult? _videosLibraryQueryResult;
        private bool _invalidateMusicCache;
        private bool _invalidateVideosCache;

        public LibraryService(IFilesService filesService, MediaViewModelFactory mediaFactory,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            _lockObject = new object();
            _songs = new List<MediaViewModel>();
            _albums = new List<AlbumViewModel>();
            _artists = new List<ArtistViewModel>();
            _videos = new List<MediaViewModel>();
        }

        public Task<MusicLibraryFetchResult> FetchMusicAsync(bool useCache = true)
        {
            lock (_lockObject)
            {
                if (_loadMusicTask is { IsCompleted: false })
                {
                    return _loadMusicTask;
                }

                return _loadMusicTask = FetchSongsInternalAsync(useCache);
            }
        }

        public Task<IReadOnlyList<MediaViewModel>> FetchVideosAsync(bool useCache = true)
        {
            lock (_lockObject)
            {
                if (_loadVideosTask is { IsCompleted: false })
                {
                    return _loadVideosTask;
                }

                return _loadVideosTask = FetchVideosInternalAsync(useCache);
            }
        }

        public async Task RemoveCacheAsync()
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            IStorageItem? item = await folder.TryGetItemAsync(MusicSaveFileName);
            if (item != null)
            {
                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                _invalidateMusicCache = true;
            }

            item = await folder.TryGetItemAsync(VideosSaveFileName);
            if (item != null)
            {
                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                _invalidateVideosCache = true;
            }
        }

        public void RemoveMedia(MediaViewModel media)
        {
            media.Album?.RelatedSongs.Remove(media);

            foreach (ArtistViewModel artist in media.Artists)
            {
                artist.RelatedSongs.Remove(media);
            }

            _songs.Remove(media);
            _videos.Remove(media);
        }

        private void RemoveMediaWithPath(string path)
        {
            MediaViewModel? media = _songs.Find(m => m.Location.Equals(path, StringComparison.OrdinalIgnoreCase));
            media ??= _videos.Find(m => m.Location.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (media != null)
            {
                RemoveMedia(media);
            }
        }

        private async Task<MusicLibraryFetchResult> FetchSongsInternalAsync(bool useCache)
        {
            List<MediaViewModel> songs = _songs;
            bool firstTime = _musicLibrary == null;
            StorageLibrary library = await InitializeMusicLibraryAsync();
            StorageFileQueryResult queryResult = GetMusicLibraryQueryResult();
            if (useCache && !_invalidateMusicCache)
            {
                if (firstTime)
                {
                    songs = await FetchMediaFromCache(MusicSaveFileName);
                    using StorageLibraryChangeResult changeResult = await GetLibraryChangeAsync(library);
                    if (songs.Count > 0 && changeResult.Status == StorageLibraryChangeStatus.HasChange)
                    {
                        await UpdateMediaList(changeResult, songs);
                        await CacheAsync(songs, MusicSaveFileName);
                        _songs = songs;
                        _artists = _artistFactory.GetAllArtists();
                        _albums = _albumFactory.GetAllAlbums();
                    }
                }

                if (songs.Count > 0)
                {
                    return new MusicLibraryFetchResult(songs.AsReadOnly(), _albums.AsReadOnly(), _artists.AsReadOnly());
                }
            }

            songs = await FetchMediaFromStorage(queryResult);
            await CacheAsync(songs, MusicSaveFileName);
            _invalidateMusicCache = false;
            _songs = songs;
            _artists = _artistFactory.GetAllArtists();
            _albums = _albumFactory.GetAllAlbums();
            return new MusicLibraryFetchResult(songs.AsReadOnly(), _albums.AsReadOnly(), _artists.AsReadOnly());
        }

        private async Task<IReadOnlyList<MediaViewModel>> FetchVideosInternalAsync(bool useCache)
        {
            List<MediaViewModel> videos = _videos;
            bool firstTime = _videosLibrary == null;
            StorageLibrary library = await InitializeVideosLibraryAsync();
            StorageFileQueryResult queryResult = GetVideosLibraryQueryResult();
            if (useCache && !_invalidateVideosCache)
            {
                if (firstTime)
                {
                    videos = await FetchMediaFromCache(VideosSaveFileName);
                    using StorageLibraryChangeResult changeResult = await GetLibraryChangeAsync(library);
                    if (videos.Count > 0 && changeResult.Status == StorageLibraryChangeStatus.HasChange)
                    {
                        await UpdateMediaList(changeResult, videos);
                        await CacheAsync(videos, VideosSaveFileName);
                        _videos = videos;
                    }
                }

                if (videos.Count > 0)
                {
                    return videos;
                }
            }

            videos = await FetchMediaFromStorage(queryResult);
            await CacheAsync(videos, VideosSaveFileName);
            _invalidateVideosCache = false;
            _videos = videos;
            return videos.AsReadOnly();
        }

        private async Task CacheAsync(IEnumerable<MediaViewModel> media, string fileName)
        {
            List<MediaFileRecord> records = media.Select(ToRecord).ToList();
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            using Stream stream = await file.OpenStreamForWriteAsync();
            Serializer.Serialize(stream, records);
            stream.SetLength(stream.Position);
        }

        private async Task<List<MediaViewModel>> FetchMediaFromStorage(StorageFileQueryResult queryResult)
        {
            List<MediaViewModel> media = new();
            uint fetchIndex = 0;
            while (fetchIndex < MaxLoadCount)
            {
                IReadOnlyList<StorageFile> files = await queryResult.GetFilesAsync(fetchIndex, 50);
                if (files.Count == 0) break;
                fetchIndex += (uint)files.Count;
                media.AddRange(files.Select(_mediaFactory.GetSingleton));
            }

            foreach (MediaViewModel song in media)
            {
                await song.LoadDetailsAsync();
            }

            return media;
        }

        private async Task<List<MediaViewModel>> FetchMediaFromCache(string fileName)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            IStorageItem? item = await folder.TryGetItemAsync(fileName);
            if (item is not StorageFile file)
            {
                return new List<MediaViewModel>();
            }

            try
            {
                using Stream readStream = await file.OpenStreamForReadAsync();
                List<MediaFileRecord> records = Serializer.Deserialize<List<MediaFileRecord>>(readStream);
                return records.Count == 0
                    ? new List<MediaViewModel>()
                    : records.Select(TryGetMedia).OfType<MediaViewModel>().ToList();
            }
            catch (Exception)
            {
                return new List<MediaViewModel>();
            }
        }

        private async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            if (_musicLibrary == null)
            {
                _musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                _musicLibrary.DefinitionChanged += OnMusicLibraryContentChanged;
                _musicLibrary.ChangeTracker.Enable();
            }

            return _musicLibrary;
        }

        private async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            if (_videosLibrary == null)
            {
                _videosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
                _videosLibrary.DefinitionChanged += OnVideosLibraryContentChanged;
                _videosLibrary.ChangeTracker.Enable();
            }

            return _videosLibrary;
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
            _invalidateVideosCache = true;
            VideosLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnMusicLibraryContentChanged(object sender, object args)
        {
            _invalidateMusicCache = true;
            MusicLibraryContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task<StorageLibraryChangeResult> GetLibraryChangeAsync(StorageLibrary library)
        {
            StorageLibraryChangeTracker changeTracker = library.ChangeTracker;
            changeTracker.Enable();
            StorageLibraryChangeReader changeReader = changeTracker.GetChangeReader();
            IReadOnlyList<StorageLibraryChange> changes = await changeReader.ReadBatchAsync();
            ulong lastChangeId = changeReader.GetLastChangeId();
            List<StorageFile> addedItems = new();
            List<string> removedItems = new();

            // Last change ID does not work correctly. ID equals to 0 when there are changes.
            if (lastChangeId != StorageLibraryLastChangeId.Unknown)
            {
                foreach (StorageLibraryChange change in changes)
                {
                    if (change.ChangeType == StorageLibraryChangeType.ChangeTrackingLost ||
                        !change.IsOfType(StorageItemTypes.File))
                    {
                        changeTracker.Reset();
                        return new StorageLibraryChangeResult(StorageLibraryChangeStatus.Unknown);
                    }

                    switch (change.ChangeType)
                    {
                        case StorageLibraryChangeType.MovedIntoLibrary:
                        case StorageLibraryChangeType.Created:
                        {
                            StorageFile file = (StorageFile)await change.GetStorageItemAsync();
                            if (!_filesService.SupportedFormats.Contains(file.FileType.ToLowerInvariant()))
                                continue;

                            addedItems.Add(file);
                            break;
                        }

                        case StorageLibraryChangeType.MovedOutOfLibrary:
                        case StorageLibraryChangeType.Deleted:
                        {
                            removedItems.Add(change.PreviousPath);
                            break;
                        }

                        case StorageLibraryChangeType.MovedOrRenamed:
                        case StorageLibraryChangeType.ContentsChanged:
                        case StorageLibraryChangeType.ContentsReplaced:
                        {
                            StorageFile file = (StorageFile)await change.GetStorageItemAsync();
                            if (!_filesService.SupportedFormats.Contains(file.FileType.ToLowerInvariant()))
                                continue;

                            removedItems.Add(file.Path);
                            addedItems.Add(file);
                            break;
                        }

                        case StorageLibraryChangeType.IndexingStatusChanged:
                        case StorageLibraryChangeType.EncryptionChanged:
                        case StorageLibraryChangeType.ChangeTrackingLost:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return new StorageLibraryChangeResult(changeReader, addedItems, removedItems);
            }

            changeTracker.Reset();
            return new StorageLibraryChangeResult(StorageLibraryChangeStatus.Unknown);
        }

        private async Task UpdateMediaList(StorageLibraryChangeResult changeResult, List<MediaViewModel> targetList)
        {
            if (changeResult.Status != StorageLibraryChangeStatus.HasChange) return;
            foreach (string removedItem in changeResult.RemovedItems)
            {
                RemoveMediaWithPath(removedItem);
            }

            List<MediaViewModel> addedItems =
                changeResult.AddedItems.Select(_mediaFactory.GetSingleton).ToList();

            // Expecting relatively small number of changes
            foreach (MediaViewModel addedItem in addedItems)
            {
                if (targetList.Contains(addedItem))
                    continue;

                await addedItem.LoadDetailsAsync();
                targetList.Add(addedItem);
            }
        }

        private MediaViewModel? TryGetMedia(MediaFileRecord record)
        {
            try
            {
                MediaViewModel media = _mediaFactory.GetSingleton(record.Path);
                if (media.Source is not Uri)
                {
                    return media;
                }

                media.Name = record.Name;
                if (record.Duration > TimeSpan.Zero)
                {
                    media.Duration = record.Duration;
                }

                media.MediaType = record.MediaType;
                if (record.MediaType == MediaPlaybackType.Music)
                {
                    media.TrackNumber = record.TrackNumber;
                    media.Genre = record.Genre;
                    media.Year = record.Year;
                    media.Album ??= _albumFactory.AddSongToAlbum(media, record.Album, record.AlbumArtist, media.Year);
                    media.Artists = _artistFactory.ParseArtists(record.Artists, media);
                }

                return media;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private MediaFileRecord ToRecord(MediaViewModel m)
        {
            string[] artistNames = m.Artists.Where(a => a != _artistFactory.UnknownArtist).Select(a => a.Name).ToArray();
            return new MediaFileRecord(
                m.Location, m.Name, m.MediaType, m.Album?.Name ?? string.Empty,
                m.MusicProperties?.AlbumArtist ?? string.Empty, artistNames,
                m.Duration ?? TimeSpan.Zero, m.TrackNumber, m.Genre ?? string.Empty, m.Year);
        }
    }
}

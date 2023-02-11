#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public IReadOnlyList<MediaViewModel> Songs => _songs.AsReadOnly();
        public IReadOnlyList<AlbumViewModel> Albums => _albums.AsReadOnly();
        public IReadOnlyList<ArtistViewModel> Artists => _artists.AsReadOnly();
        public IReadOnlyList<MediaViewModel> Videos => _videos.AsReadOnly();

        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly object _lockObject;

        private const int MaxLoadCount = 5000;
        private const string SaveFileName = "library.bin";

        private Task<IReadOnlyList<MediaViewModel>>? _loadSongsTask;
        private Task<IReadOnlyList<MediaViewModel>>? _loadVideosTask;
        private List<MediaViewModel> _songs;
        private List<AlbumViewModel> _albums;
        private List<ArtistViewModel> _artists;
        private List<MediaViewModel> _videos;
        private StorageLibrary? _musicLibrary;
        private StorageLibrary? _videosLibrary;
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

        public Task<IReadOnlyList<MediaViewModel>> FetchSongsAsync(bool useCache = true)
        {
            lock (_lockObject)
            {
                if (_loadSongsTask is { IsCompleted: false })
                {
                    return _loadSongsTask;
                }

                return _loadSongsTask = FetchSongsInternalAsync(useCache);
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

        public void RemoveMedia(MediaViewModel media)
        {
            media.Album?.RelatedSongs.Remove(media);

            foreach (ArtistViewModel artist in Artists)
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

        private async Task<IReadOnlyList<MediaViewModel>> FetchSongsInternalAsync(bool useCache)
        {
            List<MediaViewModel> songs = _songs;
            bool firstTime = _musicLibrary == null;
            StorageLibrary library = await InitializeMusicLibraryAsync();
            if (!_invalidateMusicCache && useCache)
            {
                // if (firstTime)
                // {
                //     songs = await FetchSongsFromCache();
                // }

                using StorageLibraryChangeResult changeResult = await GetLibraryChangeAsync(library);
                if (songs.Count > 0 && changeResult.Status != StorageLibraryChangeStatus.Unknown)
                {
                    if (changeResult.Status == StorageLibraryChangeStatus.HasChange)
                    {
                        await UpdateMediaList(changeResult, songs);
                        _artists = _artistFactory.GetAllArtists();
                        _albums = _albumFactory.GetAllAlbums();
                    }

                    return songs.AsReadOnly();
                }

                songs = await FetchMediaFromStorage(KnownLibraryId.Music);
                // await CacheAsync(songs);
            }
            else
            {
                songs = await FetchMediaFromStorage(KnownLibraryId.Music);
                // await CacheAsync(songs);
            }

            _songs = songs;
            _artists = _artistFactory.GetAllArtists();
            _albums = _albumFactory.GetAllAlbums();
            return songs.AsReadOnly();
        }

        private async Task<IReadOnlyList<MediaViewModel>> FetchVideosInternalAsync(bool useCache)
        {
            List<MediaViewModel> videos;
            StorageLibrary library = await InitializeVideosLibraryAsync();
            if (!_invalidateVideosCache && useCache)
            {
                using StorageLibraryChangeResult changeResult = await GetLibraryChangeAsync(library);
                if (_videos.Count > 0)
                {
                    await UpdateMediaList(changeResult, _videos);
                    return _videos.AsReadOnly();
                }

                videos = await FetchMediaFromStorage(KnownLibraryId.Videos);
            }
            else
            {
                videos = await FetchMediaFromStorage(KnownLibraryId.Videos);
            }

            _videos = videos;
            return videos.AsReadOnly();
        }

        private async Task CacheAsync(IEnumerable<MediaViewModel> media)
        {
            List<MediaFileRecord> records = media.Select(ToRecord).ToList();
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.CreateFileAsync(SaveFileName, CreationCollisionOption.OpenIfExists);
            using Stream stream = await file.OpenStreamForWriteAsync();
            Serializer.Serialize(stream, records);
            stream.SetLength(stream.Position);
        }

        private async Task<List<MediaViewModel>> FetchMediaFromStorage(KnownLibraryId libraryId)
        {
            if (libraryId != KnownLibraryId.Music && libraryId != KnownLibraryId.Videos)
                throw new ArgumentOutOfRangeException(nameof(libraryId));

            StorageFileQueryResult queryResult = libraryId == KnownLibraryId.Music
                ? _filesService.GetSongsFromLibrary()
                : _filesService.GetVideosFromLibrary();
            return await FetchMediaFromStorage(queryResult);
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

        private async Task<List<MediaViewModel>> FetchSongsFromCache()
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            IStorageItem? item = await folder.TryGetItemAsync(SaveFileName);
            if (item is not StorageFile file)
            {
                return new List<MediaViewModel>(0);
            }

            try
            {
                using Stream readStream = await file.OpenStreamForReadAsync();
                List<MediaFileRecord> records = Serializer.Deserialize<List<MediaFileRecord>>(readStream);
                return records.Count == 0
                    ? new List<MediaViewModel>(0)
                    : records.Select(TryGetMedia).OfType<MediaViewModel>().ToList();
            }
            catch (Exception)
            {
                return new List<MediaViewModel>(0);
            }
        }

        private async Task<StorageLibrary> InitializeMusicLibraryAsync()
        {
            if (_musicLibrary == null)
            {
                _musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                _musicLibrary.DefinitionChanged += (_, _) => _invalidateMusicCache = true;
            }

            return _musicLibrary;
        }

        private async Task<StorageLibrary> InitializeVideosLibraryAsync()
        {
            if (_videosLibrary == null)
            {
                _videosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
                _videosLibrary.DefinitionChanged += (_, _) => _invalidateVideosCache = true;
            }

            return _videosLibrary;
        }

        private async Task<StorageLibraryChangeResult> GetLibraryChangeAsync(StorageLibrary library)
        {
            StorageLibraryChangeTracker changeTracker = library.ChangeTracker;
            changeTracker.Enable();
            StorageLibraryChangeReader changeReader = changeTracker.GetChangeReader();
            ulong lastChangeId = changeReader.GetLastChangeId();
            List<StorageFile> addedItems = new();
            List<string> removedItems = new();
            if (lastChangeId > 0 && lastChangeId != StorageLibraryLastChangeId.Unknown)
            {
                IReadOnlyList<StorageLibraryChange> changes = await changeReader.ReadBatchAsync();
                foreach (StorageLibraryChange change in changes)
                {
                    if (change.ChangeType == StorageLibraryChangeType.ChangeTrackingLost ||
                        !change.IsOfType(StorageItemTypes.File))
                    {
                        return new StorageLibraryChangeResult(StorageLibraryChangeStatus.Unknown, changeReader);
                    }

                    switch (change.ChangeType)
                    {
                        case StorageLibraryChangeType.MovedIntoLibrary:
                        case StorageLibraryChangeType.Created:
                        {
                            StorageFile file = (StorageFile)await change.GetStorageItemAsync();
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
            }

            return new StorageLibraryChangeResult(changeReader, addedItems, removedItems);
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
            foreach (MediaViewModel addedItem in addedItems)
            {
                await addedItem.LoadDetailsAsync();
            }

            targetList.AddRange(addedItems);
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

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

        private async Task<IReadOnlyList<MediaViewModel>> FetchSongsInternalAsync(bool useCache)
        {
            List<MediaViewModel> songs;
            if (useCache)
            {
                if (Songs.Count > 0) return Songs;
                songs = await FetchSongsFromStorage();
                // songs = await FetchSongsFromCache();
                // if (songs.Count == 0)
                // {
                //     songs = await FetchSongsFromStorage();
                //     await CacheAsync(songs);
                // }
            }
            else
            {
                songs = await FetchSongsFromStorage();
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
            if (useCache)
            {
                if (Videos.Count > 0) return Videos;
                videos = await FetchVideosFromStorage();
            }
            else
            {
                videos = await FetchVideosFromStorage();
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

        private async Task<List<MediaViewModel>> FetchSongsFromStorage()
        {
            StorageFileQueryResult queryResult = _filesService.GetSongsFromLibrary();
            return await FetchMediaFromStorage(queryResult);
        }

        private async Task<List<MediaViewModel>> FetchVideosFromStorage()
        {
            StorageFileQueryResult queryResult = _filesService.GetVideosFromLibrary();
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

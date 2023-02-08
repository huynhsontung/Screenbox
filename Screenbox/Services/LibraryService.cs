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
        private readonly IFilesService _filesService;
        private readonly MediaViewModelFactory _mediaFactory;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly object _lockObject;

        private const int MaxLoadCount = 5000;
        private const string SaveFileName = "library.bin";

        private IList<MediaViewModel>? _songs;
        private Task<IList<MediaViewModel>>? _loadSongsTask;

        public LibraryService(IFilesService filesService, MediaViewModelFactory mediaFactory,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _filesService = filesService;
            _mediaFactory = mediaFactory;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            _lockObject = new object();
        }

        public Task<IList<MediaViewModel>> FetchSongsAsync(bool ignoreCache = false)
        {
            lock (_lockObject)
            {
                if (_loadSongsTask is { IsCompleted: false })
                {
                    return _loadSongsTask;
                }

                return _loadSongsTask = FetchSongsInternalAsync(ignoreCache);
            }
        }

        private async Task<IList<MediaViewModel>> FetchSongsInternalAsync(bool ignoreCache)
        {
            IList<MediaViewModel> songs;
            if (ignoreCache)
            {
                songs = await FetchSongsFromStorage();
                await CacheAsync(songs);
            }
            else
            {
                if (_songs != null) return _songs;
                songs = await FetchSongsFromCache();
                if (songs.Count == 0)
                {
                    songs = await FetchSongsFromStorage();
                    await CacheAsync(songs);
                }
            }

            return _songs = songs;
        }

        private async Task CacheAsync(IList<MediaViewModel> media)
        {
            List<MediaFileRecord> records = media.Select(ToRecord).ToList();
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.CreateFileAsync(SaveFileName, CreationCollisionOption.OpenIfExists);
            using Stream stream = await file.OpenStreamForWriteAsync();
            Serializer.Serialize(stream, records);
            stream.SetLength(stream.Position);
        }

        private async Task<IList<MediaViewModel>> FetchSongsFromStorage()
        {
            StorageFileQueryResult queryResult = _filesService.GetSongsFromLibrary();
            List<MediaViewModel> songs = new();
            uint fetchIndex = 0;
            while (fetchIndex < MaxLoadCount)
            {
                IReadOnlyList<StorageFile> files = await queryResult.GetFilesAsync(fetchIndex, 50);
                if (files.Count == 0) break;
                fetchIndex += (uint)files.Count;
                songs.AddRange(files.Select(_mediaFactory.GetSingleton));
            }

            foreach (MediaViewModel song in songs)
            {
                await song.LoadDetailsAsync();
            }

            return songs;
        }

        private async Task<IList<MediaViewModel>> FetchSongsFromCache()
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

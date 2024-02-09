#nullable enable

using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Factories
{
    public sealed class AlbumViewModelFactory
    {
        public AlbumViewModel UnknownAlbum { get; }

        public IReadOnlyCollection<AlbumViewModel> AllAlbums { get; }

        private readonly Dictionary<string, AlbumViewModel> _allAlbums;
        private readonly IResourceService _resourceService;

        public AlbumViewModelFactory(IResourceService resourceService)
        {
            _resourceService = resourceService;
            UnknownAlbum = new AlbumViewModel(resourceService.GetString(ResourceName.UnknownAlbum), resourceService.GetString(ResourceName.UnknownArtist));
            _allAlbums = new Dictionary<string, AlbumViewModel>();
            AllAlbums = _allAlbums.Values;
        }

        public AlbumViewModel GetAlbumFromName(string albumName, string artistName)
        {
            if (string.IsNullOrEmpty(albumName) || albumName == _resourceService.GetString(ResourceName.UnknownAlbum))
            {
                return UnknownAlbum;
            }

            string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string artistKey = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string key = GetAlbumKey(albumKey, artistKey);
            return _allAlbums.TryGetValue(key, out AlbumViewModel album) ? album : UnknownAlbum;
        }

        public AlbumViewModel AddSongToAlbum(MediaViewModel song, string albumName, string artistName, uint year)
        {
            if (string.IsNullOrEmpty(albumName))
            {
                UnknownAlbum.RelatedSongs.Add(song);
                return UnknownAlbum;
            }

            AlbumViewModel album = GetAlbumFromName(albumName, artistName);
            if (album != UnknownAlbum)
            {
                album.Year ??= year;
                album.RelatedSongs.Add(song);
                return album;
            }

            string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string artistKey = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string key = GetAlbumKey(albumKey, artistKey);
            album = new AlbumViewModel(albumName, artistName)
            {
                Year = year
            };

            album.RelatedSongs.Add(song);
            return _allAlbums[key] = album;
        }

        public void Remove(MediaViewModel song)
        {
            AlbumViewModel? album = song.Album;
            if (album == null) return;
            song.Album = null;
            album.RelatedSongs.Remove(song);
            if (album.RelatedSongs.Count == 0)
            {
                string albumKey = album.Name.Trim().ToLower(CultureInfo.CurrentUICulture);
                string artistKey = album.ArtistName.Trim().ToLower(CultureInfo.CurrentUICulture);
                _allAlbums.Remove(GetAlbumKey(albumKey, artistKey));
            }
        }

        public void Compact()
        {
            List<string> albumKeysToRemove =
                _allAlbums.Where(p => p.Value.RelatedSongs.Count == 0).Select(p => p.Key).ToList();

            foreach (string albumKey in albumKeysToRemove)
            {
                _allAlbums.Remove(albumKey);
            }
        }

        public void Clear()
        {
            foreach (MediaViewModel media in UnknownAlbum.RelatedSongs)
            {
                media.Album = null;
            }

            UnknownAlbum.RelatedSongs.Clear();

            foreach ((string _, AlbumViewModel album) in _allAlbums)
            {
                foreach (MediaViewModel media in album.RelatedSongs)
                {
                    media.Album = null;
                }
            }

            _allAlbums.Clear();
        }

        private static string GetAlbumKey(string albumName, string artistName)
        {
            return $"{albumName};{artistName}";
        }
    }
}

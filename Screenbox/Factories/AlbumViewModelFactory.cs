#nullable enable

using Screenbox.ViewModels;
using System.Collections.Generic;
using System.Globalization;

namespace Screenbox.Factories
{
    internal sealed class AlbumViewModelFactory
    {
        private readonly Dictionary<string, AlbumViewModel> _allAlbums;
        private readonly AlbumViewModel _unknownAlbum;

        public AlbumViewModelFactory()
        {
            _unknownAlbum = new AlbumViewModel(Strings.Resources.UnknownAlbum, Strings.Resources.UnknownArtist);
            _allAlbums = new Dictionary<string, AlbumViewModel>();
        }

        public AlbumViewModel GetAlbumFromName(string albumName, string artistName)
        {
            if (string.IsNullOrEmpty(albumName) || albumName == Strings.Resources.UnknownAlbum)
            {
                return _unknownAlbum;
            }

            string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string artistKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string key = GetAlbumKey(albumKey, artistKey);
            return _allAlbums.TryGetValue(key, out AlbumViewModel album) ? album : _unknownAlbum;
        }

        public AlbumViewModel AddSongToAlbum(MediaViewModel song, string? albumName = null, string? artistName = null)
        {
            albumName ??= song.MusicProperties?.Album ?? string.Empty;
            artistName ??= song.MusicProperties?.AlbumArtist ?? string.Empty;
            if (string.IsNullOrEmpty(albumName))
            {
                _unknownAlbum.RelatedSongs.Add(song);
                return _unknownAlbum;
            }

            AlbumViewModel album = GetAlbumFromName(albumName, artistName);
            if (album != _unknownAlbum)
            {
                album.Year ??= song.MusicProperties?.Year;
                album.RelatedSongs.Add(song);
                return album;
            }

            string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string artistKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string key = GetAlbumKey(albumKey, artistKey);
            album = new AlbumViewModel(albumName, artistName)
            {
                Year = song.MusicProperties?.Year
            };

            album.RelatedSongs.Add(song);
            return _allAlbums[key] = album;
        }

        private static string GetAlbumKey(string albumName, string artistName)
        {
            return $"{albumName};{artistName}";
        }
    }
}

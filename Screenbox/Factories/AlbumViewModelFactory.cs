#nullable enable

using Screenbox.ViewModels;
using System.Collections.Generic;
using System.Globalization;

namespace Screenbox.Factories
{
    internal sealed class AlbumViewModelFactory
    {
        public AlbumViewModel UnknownAlbum { get; }

        private readonly Dictionary<string, AlbumViewModel> _allAlbums;

        public AlbumViewModelFactory()
        {
            UnknownAlbum = new AlbumViewModel(Strings.Resources.UnknownAlbum, Strings.Resources.UnknownArtist);
            _allAlbums = new Dictionary<string, AlbumViewModel>();
        }

        public AlbumViewModel GetAlbumFromName(string albumName, string artistName)
        {
            if (string.IsNullOrEmpty(albumName) || albumName == Strings.Resources.UnknownAlbum)
            {
                return UnknownAlbum;
            }

            string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string artistKey = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string key = GetAlbumKey(albumKey, artistKey);
            return _allAlbums.TryGetValue(key, out AlbumViewModel album) ? album : UnknownAlbum;
        }

        public AlbumViewModel AddSongToAlbum(MediaViewModel song, string? albumName = null, string? artistName = null, uint year = 0)
        {
            albumName ??= song.MusicProperties?.Album ?? string.Empty;
            artistName ??= song.MusicProperties?.AlbumArtist ?? string.Empty;
            if (year == 0 && song.MusicProperties != null)
            {
                year = song.MusicProperties.Year;
            }

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

        private static string GetAlbumKey(string albumName, string artistName)
        {
            return $"{albumName};{artistName}";
        }
    }
}

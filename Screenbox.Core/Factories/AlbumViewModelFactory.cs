#nullable enable

using Screenbox.Core.Contexts;
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
        public AlbumViewModel UnknownAlbum => State.UnknownAlbum!;

        public IReadOnlyCollection<AlbumViewModel> AllAlbums => State.Albums.Values;
        private readonly IResourceService _resourceService;
        private readonly AlbumFactoryContext State;

        public AlbumViewModelFactory(IResourceService resourceService, AlbumFactoryContext state)
        {
            _resourceService = resourceService;
            State = state;
            State.UnknownAlbum ??= new AlbumViewModel(resourceService.GetString(ResourceName.UnknownAlbum), resourceService.GetString(ResourceName.UnknownArtist));
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
            return State.Albums.GetValueOrDefault(key, UnknownAlbum);
        }

        public AlbumViewModel AddSongToAlbum(MediaViewModel song, string albumName, string artistName, uint year)
        {
            if (string.IsNullOrEmpty(albumName))
            {
                UnknownAlbum.RelatedSongs.Add(song);
                UpdateAlbumDateAdded(UnknownAlbum, song);
                return UnknownAlbum;
            }

            AlbumViewModel album = GetAlbumFromName(albumName, artistName);
            if (album != UnknownAlbum)
            {
                album.Year ??= year;
                album.RelatedSongs.Add(song);
                UpdateAlbumDateAdded(album, song);
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
            UpdateAlbumDateAdded(album, song);
            return State.Albums[key] = album;
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
                State.Albums.Remove(GetAlbumKey(albumKey, artistKey));
            }
        }

        public void Compact()
        {
            List<string> albumKeysToRemove =
                State.Albums.Where(p => p.Value.RelatedSongs.Count == 0).Select(p => p.Key).ToList();

            foreach (string albumKey in albumKeysToRemove)
            {
                State.Albums.Remove(albumKey);
            }
        }

        public void Clear()
        {
            foreach (MediaViewModel media in UnknownAlbum.RelatedSongs)
            {
                media.Album = null;
            }

            UnknownAlbum.RelatedSongs.Clear();
            UnknownAlbum.DateAdded = default;

            foreach ((string _, AlbumViewModel album) in State.Albums)
            {
                foreach (MediaViewModel media in album.RelatedSongs)
                {
                    media.Album = null;
                }
            }

            State.Albums.Clear();
        }

        private static void UpdateAlbumDateAdded(AlbumViewModel album, MediaViewModel song)
        {
            if (song.DateAdded == default) return;
            if (album.DateAdded > song.DateAdded || album.DateAdded == default) album.DateAdded = song.DateAdded;
        }

        private static string GetAlbumKey(string albumName, string artistName)
        {
            return $"{albumName};{artistName}";
        }
    }
}

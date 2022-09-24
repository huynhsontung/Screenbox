#nullable enable

using System;
using System.Collections.Generic;

namespace Screenbox.ViewModels
{
    internal sealed class AlbumViewModel
    {
        private static readonly Dictionary<Tuple<string, string>, AlbumViewModel> AllAlbums = new()
        {
            { new Tuple<string, string>(Strings.Resources.UnknownAlbum, Strings.Resources.UnknownArtist), new AlbumViewModel(Strings.Resources.UnknownAlbum, Strings.Resources.UnknownArtist) }
        };

        public string Name { get; }

        public string Artist { get; }

        public List<MediaViewModel> RelatedSongs { get; }

        private AlbumViewModel(string album, string albumArtist)
        {
            Name = album;
            Artist = albumArtist;
            RelatedSongs = new List<MediaViewModel>();
        }

        public static AlbumViewModel GetAlbumForSong(MediaViewModel song, string album, string artist)
        {
            // Assume each song will only call this method once for each contributing artist
            Tuple<string, string> key = new(Strings.Resources.UnknownAlbum, Strings.Resources.UnknownArtist);
            if (string.IsNullOrEmpty(album))
            {
                AlbumViewModel unknownAlbum = AllAlbums[key];
                unknownAlbum.RelatedSongs.Add(song);
                return unknownAlbum;
            }

            key = new Tuple<string, string>(album, artist);
            if (!AllAlbums.TryGetValue(key, out AlbumViewModel albumVm))
            {
                albumVm = AllAlbums[key] = new AlbumViewModel(album, artist);
            }

            albumVm.RelatedSongs.Add(song);
            return albumVm;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

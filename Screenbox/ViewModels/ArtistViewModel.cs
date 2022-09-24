#nullable enable

using System.Collections.Generic;

namespace Screenbox.ViewModels
{
    internal sealed class ArtistViewModel
    {
        private static readonly Dictionary<string, ArtistViewModel> AllArtists = new()
            { { Strings.Resources.UnknownArtist, new ArtistViewModel(Strings.Resources.UnknownArtist) } };

        public List<MediaViewModel> RelatedSongs { get; }

        public string Name { get; }

        private ArtistViewModel(string artist)
        {
            Name = artist;
            RelatedSongs = new List<MediaViewModel>();
        }

        public static ArtistViewModel GetArtistForSong(MediaViewModel song, string artistName)
        {
            // Assume each song will only call this method once for each contributing artist
            if (string.IsNullOrEmpty(artistName))
            {
                ArtistViewModel unknownArtist = AllArtists[Strings.Resources.UnknownArtist];
                unknownArtist.RelatedSongs.Add(song);
                return unknownArtist;
            }

            if (!AllArtists.TryGetValue(artistName, out ArtistViewModel artist))
            {
                artist = AllArtists[artistName] = new ArtistViewModel(artistName);
            }

            artist.RelatedSongs.Add(song);
            return artist;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

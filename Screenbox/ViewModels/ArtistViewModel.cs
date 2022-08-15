#nullable enable

using System.Collections.Generic;

namespace Screenbox.ViewModels
{
    internal class ArtistViewModel
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

        public static ArtistViewModel[] GetArtistList(MediaViewModel song, string[]? contributingArtists)
        {
            if (contributingArtists == null || contributingArtists.Length == 0)
            {
                ArtistViewModel unknownArtist = AllArtists[Strings.Resources.UnknownArtist];
                unknownArtist.RelatedSongs.Add(song);
                return new[] { unknownArtist };
            }

            ArtistViewModel[] artistList = new ArtistViewModel[contributingArtists.Length];
            for (int i = 0; i < contributingArtists.Length; i++)
            {
                string contributingArtist = contributingArtists[i];
                if (!AllArtists.TryGetValue(contributingArtist, out ArtistViewModel artist))
                {
                    artist = AllArtists[contributingArtist] = new ArtistViewModel(contributingArtist);
                }

                artist.RelatedSongs.Add(song);
                artistList[i] = artist;
            }

            return artistList;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

#nullable enable

using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Factories
{
    public sealed class ArtistViewModelFactory
    {
        public ArtistViewModel UnknownArtist { get; }

        public IReadOnlyCollection<ArtistViewModel> AllArtists { get; }

        private readonly Dictionary<string, ArtistViewModel> _allArtists;

        private static readonly string[] ArtistNameSeparators = { ",", ", ", "; " };

        public ArtistViewModelFactory(IResourceService resourceService)
        {
            _allArtists = new Dictionary<string, ArtistViewModel>();
            AllArtists = _allArtists.Values;
            UnknownArtist = new ArtistViewModel(resourceService.GetString(ResourceName.UnknownArtist));
        }

        public ArtistViewModel[] ParseArtists(string artist, MediaViewModel song)
        {
            return ParseArtists(artist.Split(ArtistNameSeparators, StringSplitOptions.RemoveEmptyEntries), song);
        }

        public ArtistViewModel[] ParseArtists(string[] artists, MediaViewModel song)
        {
            if (artists.Length == 0)
            {
                AddSongToArtist(song, string.Empty);
                return new[] { UnknownArtist };
            }

            IEnumerable<string> artistNames = artists;
            if (artists.Length == 1)
            {
                string artistName = artists[0];
                string[] splits = artistName.Split(ArtistNameSeparators, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length > 1)
                {
                    artistNames = splits.Prepend(artistName);
                }
            }

            return artistNames
                .Select(artist => AddSongToArtist(song, artist.Trim()))
                .ToArray();
        }

        public ArtistViewModel GetArtistFromName(string artistName)
        {
            if (string.IsNullOrEmpty(artistName))
                return UnknownArtist;

            string key = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            return _allArtists.TryGetValue(key, out ArtistViewModel artist) ? artist : UnknownArtist;
        }

        public ArtistViewModel AddSongToArtist(MediaViewModel song, string artistName)
        {
            if (string.IsNullOrEmpty(artistName))
            {
                UnknownArtist.RelatedSongs.Add(song);
                return UnknownArtist;
            }

            ArtistViewModel artist = GetArtistFromName(artistName);
            if (artist != UnknownArtist)
            {
                artist.RelatedSongs.Add(song);
                return artist;
            }

            string key = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            artist = new ArtistViewModel(artistName);
            artist.RelatedSongs.Add(song);
            return _allArtists[key] = artist;
        }
    }
}

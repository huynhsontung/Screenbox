#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Screenbox.Core.ViewModels;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Factories;

public sealed class ArtistViewModelFactory
{
    public ArtistViewModel UnknownArtist { get; } = new();

    public Dictionary<string, ArtistViewModel> Artists { get; } = new();

    public Dictionary<MediaViewModel, List<ArtistViewModel>> SongsToArtists { get; } = new();

    private static readonly string[] ArtistNameSeparators = { ",", ", ", "; " };

    public void AddSong(MediaViewModel song)
    {
        if (song.MediaType != Enums.MediaPlaybackType.Music || SongsToArtists.ContainsKey(song)) return;
        var artistNames = song.MediaInfo.MusicProperties.Artist.Split(ArtistNameSeparators, StringSplitOptions.RemoveEmptyEntries);
        ParseAddArtists(artistNames, song);
    }

    public ArtistViewModel[] ParseArtists(string artist)
    {
        ArtistViewModel[] artists = artist.Split(ArtistNameSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(GetArtistFromName)
            .ToArray();

        return artists.Length == 0 ? new[] { UnknownArtist } : artists;
    }

    private ArtistViewModel[] ParseAddArtists(string[] artists, MediaViewModel song)
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

    private ArtistViewModel GetArtistFromName(string artistName)
    {
        var key = GetArtistKey(artistName);
        return Artists.GetValueOrDefault(key, UnknownArtist);
    }

    public ArtistViewModel AddSongToArtist(MediaViewModel song, string artistName)
    {
        var key = GetArtistKey(artistName);
        if (string.IsNullOrEmpty(key))
        {
            UnknownArtist.RelatedSongs.Add(song);
            SongsToArtists[song] = [UnknownArtist];
            return Artists[key] = UnknownArtist;
        }

        if (!Artists.TryGetValue(key, out var artist))
        {
            artist = new ArtistViewModel(artistName);
        }

        artist.RelatedSongs.Add(song);
        UpdateSongsToArtistMapping(song, artist);
        return Artists[key] = artist;
    }

    private void UpdateSongsToArtistMapping(MediaViewModel song, ArtistViewModel artist)
    {
        if (SongsToArtists.TryGetValue(song, out var artists))
        {
            if (!artists.Contains(artist))
            {
                artists.Add(artist);
            }
        }
        else
        {
            SongsToArtists[song] = [artist];
        }
    }

    public void Remove(MediaViewModel song, IReadOnlyList<ArtistViewModel> artists)
    {
        foreach (ArtistViewModel artist in artists)
        {
            artist.RelatedSongs.Remove(song);
            if (artist.RelatedSongs.Count == 0)
            {
                string artistKey = GetArtistKey(artist.Name);
                Artists.Remove(artistKey);
            }

            if (SongsToArtists.TryGetValue(song, out var artistsMapping))
            {
                artistsMapping.Remove(artist);
            }
        }
    }

    public string GetArtistKey(string artistName)
    {
        return artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
    }
}

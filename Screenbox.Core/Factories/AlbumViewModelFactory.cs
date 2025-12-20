#nullable enable

using System.Collections.Generic;
using System.Globalization;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Factories;

public sealed class AlbumViewModelFactory
{
    public AlbumViewModel UnknownAlbum { get; } = new();

    public Dictionary<string, AlbumViewModel> Albums { get; }

    public Dictionary<MediaViewModel, AlbumViewModel> SongsToAlbums { get; } = new();

    public AlbumViewModelFactory()
    {
        Albums = new Dictionary<string, AlbumViewModel>() { { string.Empty, UnknownAlbum } };
    }

    public AlbumViewModel GetAlbumFromName(string albumName, string artistName)
    {
        string key = GetAlbumKey(albumName, artistName);
        return Albums.GetValueOrDefault(key, UnknownAlbum);
    }

    public void AddSong(MediaViewModel song)
    {
        if (song.MediaType != MediaPlaybackType.Music || SongsToAlbums.ContainsKey(song)) return;
        MusicInfo musicProperties = song.MediaInfo.MusicProperties;
        AddSongToAlbum(song, musicProperties.Album, musicProperties.AlbumArtist, musicProperties.Year);
    }

    private AlbumViewModel AddSongToAlbum(MediaViewModel song, string albumName, string artistName, uint year)
    {
        if (string.IsNullOrEmpty(albumName))
        {
            UnknownAlbum.RelatedSongs.Add(song);
            SongsToAlbums[song] = UnknownAlbum;
            UpdateAlbumDateAdded(UnknownAlbum, song);
            return UnknownAlbum;
        }

        AlbumViewModel album = GetAlbumFromName(albumName, artistName);
        if (album != UnknownAlbum)
        {
            album.Year ??= year;
            album.RelatedSongs.Add(song);
            SongsToAlbums[song] = album;
            UpdateAlbumDateAdded(album, song);
            return album;
        }

        string key = GetAlbumKey(albumName, artistName);
        album = new AlbumViewModel(albumName, artistName)
        {
            Year = year
        };

        album.RelatedSongs.Add(song);
        SongsToAlbums[song] = album;
        UpdateAlbumDateAdded(album, song);
        return Albums[key] = album;
    }

    public void Remove(MediaViewModel song, AlbumViewModel album)
    {
        album.RelatedSongs.Remove(song);
        if (album.RelatedSongs.Count == 0)
        {
            var key = GetAlbumKey(album.Name, album.ArtistName);
            Albums.Remove(key);
        }

        SongsToAlbums.Remove(song);
    }

    private static void UpdateAlbumDateAdded(AlbumViewModel album, MediaViewModel song)
    {
        if (song.DateAdded == default) return;
        if (album.DateAdded > song.DateAdded || album.DateAdded == default) album.DateAdded = song.DateAdded;
    }

    public static string GetAlbumKey(string albumName, string artistName)
    {
        string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
        string artistKey = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
        return string.IsNullOrEmpty(albumKey) ? string.Empty : $"{albumKey};{artistKey}";
    }
}

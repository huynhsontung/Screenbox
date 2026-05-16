#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// A snapshot of the user's music library containing all songs, albums, and artists.
/// Replace the entire instance when the library is updated rather than modifying individual collections.
/// </summary>
public sealed class MusicLibrary
{
    public static readonly MusicLibrary Empty = new MusicLibrary(
        new List<MediaViewModel>(),
        new Dictionary<string, AlbumViewModel>(),
        new Dictionary<string, ArtistViewModel>(),
        new AlbumViewModel(),
        new ArtistViewModel());

    public MusicLibrary(
        IReadOnlyList<MediaViewModel> songs,
        IReadOnlyDictionary<string, AlbumViewModel> albums,
        IReadOnlyDictionary<string, ArtistViewModel> artists,
        AlbumViewModel unknownAlbum,
        ArtistViewModel unknownArtist)
    {
        Songs = songs;
        Albums = albums;
        Artists = artists;
        UnknownAlbum = unknownAlbum;
        UnknownArtist = unknownArtist;
    }

    public IReadOnlyList<MediaViewModel> Songs { get; }
    public IReadOnlyDictionary<string, AlbumViewModel> Albums { get; }
    public IReadOnlyDictionary<string, ArtistViewModel> Artists { get; }
    public AlbumViewModel UnknownAlbum { get; }
    public ArtistViewModel UnknownArtist { get; }
}

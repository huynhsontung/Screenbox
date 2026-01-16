using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

public class MusicLibrary
{
    public List<MediaViewModel> Songs { get; }

    public Dictionary<string, AlbumViewModel> Albums { get; }

    public Dictionary<string, ArtistViewModel> Artists { get; }

    public AlbumViewModel UnknownAlbum { get; }

    public ArtistViewModel UnknownArtist { get; }

    public MusicLibrary() : this(
        new List<MediaViewModel>(),
        new Dictionary<string, AlbumViewModel>(),
        new Dictionary<string, ArtistViewModel>(),
        new AlbumViewModel(),
        new ArtistViewModel()
        )
    { }

    public MusicLibrary(
        List<MediaViewModel> songs,
        Dictionary<string, AlbumViewModel> albums,
        Dictionary<string, ArtistViewModel> artists,
        AlbumViewModel unknownAlbum,
        ArtistViewModel unknownArtist
        )
    {
        Songs = songs;
        Albums = albums;
        Artists = artists;
        UnknownAlbum = unknownAlbum;
        UnknownArtist = unknownArtist;
    }
}

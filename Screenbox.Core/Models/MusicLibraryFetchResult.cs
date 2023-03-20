using System.Collections.Generic;
using Screenbox.Core.ViewModels;

using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Models
{
    public class MusicLibraryFetchResult
    {
        public MusicLibraryFetchResult(IReadOnlyList<MediaViewModel> songs, IReadOnlyList<AlbumViewModel> albums,
            IReadOnlyList<ArtistViewModel> artists, AlbumViewModel unknownAlbum, ArtistViewModel unknownArtist)
        {
            Songs = songs;
            Albums = albums;
            Artists = artists;
            UnknownAlbum = unknownAlbum;
            UnknownArtist = unknownArtist;
        }

        public IReadOnlyList<MediaViewModel> Songs { get; }

        public IReadOnlyList<AlbumViewModel> Albums { get; }

        public IReadOnlyList<ArtistViewModel> Artists { get; }

        public AlbumViewModel UnknownAlbum { get; }

        public ArtistViewModel UnknownArtist { get; }
    }
}

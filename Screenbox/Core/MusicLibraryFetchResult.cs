using System.Collections.Generic;
using Screenbox.ViewModels;

namespace Screenbox.Core
{
    internal readonly struct MusicLibraryFetchResult
    {
        public MusicLibraryFetchResult(IReadOnlyList<MediaViewModel> songs, IReadOnlyList<AlbumViewModel> albums,
            IReadOnlyList<ArtistViewModel> artists)
        {
            Songs = songs;
            Albums = albums;
            Artists = artists;
        }

        public IReadOnlyList<MediaViewModel> Songs { get; }

        public IReadOnlyList<AlbumViewModel> Albums { get; }

        public IReadOnlyList<ArtistViewModel> Artists { get; }
    }
}

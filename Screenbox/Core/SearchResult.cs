using System.Collections.Generic;
using Screenbox.ViewModels;

namespace Screenbox.Core
{
    internal class SearchResult
    {
        public SearchResult(IReadOnlyList<MediaViewModel> songs, IReadOnlyList<MediaViewModel> videos,
            IReadOnlyList<ArtistViewModel> artists, IReadOnlyList<AlbumViewModel> albums)
        {
            Songs = songs;
            Videos = videos;
            Artists = artists;
            Albums = albums;
        }

        public IReadOnlyList<MediaViewModel> Songs { get; }

        public IReadOnlyList<MediaViewModel> Videos { get; }

        public IReadOnlyList<ArtistViewModel> Artists { get; }

        public IReadOnlyList<AlbumViewModel> Albums { get; }

        public bool HasItems => Songs.Count > 0 || Videos.Count > 0 || Artists.Count > 0 || Albums.Count > 0;
    }
}

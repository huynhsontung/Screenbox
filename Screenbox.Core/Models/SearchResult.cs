using System.Collections.Generic;
using Screenbox.ViewModels;

namespace Screenbox.Core
{
    public class SearchResult
    {
        public SearchResult(string query, IReadOnlyList<MediaViewModel> songs, IReadOnlyList<MediaViewModel> videos,
            IReadOnlyList<ArtistViewModel> artists, IReadOnlyList<AlbumViewModel> albums)
        {
            Query = query;
            Songs = songs;
            Videos = videos;
            Artists = artists;
            Albums = albums;
        }

        public string Query { get; }

        public IReadOnlyList<MediaViewModel> Songs { get; }

        public IReadOnlyList<MediaViewModel> Videos { get; }

        public IReadOnlyList<ArtistViewModel> Artists { get; }

        public IReadOnlyList<AlbumViewModel> Albums { get; }

        public bool HasItems => Songs.Count > 0 || Videos.Count > 0 || Artists.Count > 0 || Albums.Count > 0;
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Screenbox.Core;
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal class SearchService : ISearchService
    {
        private readonly ILibraryService _libraryService;

        public SearchService(ILibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        public SearchResult SearchLocalLibrary(string query)
        {
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicCache();
            IReadOnlyList<MediaViewModel> videosLibrary = _libraryService.GetVideosCache();

            ImmutableList<MediaViewModel> songs = musicLibrary.Songs
                .Select(m => (Song: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Song)
                .ToImmutableList();
            ImmutableList<AlbumViewModel> albums = musicLibrary.Albums
                .Select(a => (Album: a, Index: a.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Album)
                .ToImmutableList();
            ImmutableList<ArtistViewModel> artists = musicLibrary.Artists
                .Select(a => (Artist: a, Index: a.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Artist)
                .ToImmutableList();
            ImmutableList<MediaViewModel> videos = videosLibrary
                .Select(m => (Video: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Video)
                .ToImmutableList();

            return new SearchResult(query, songs, videos, artists, albums);
        }
    }
}

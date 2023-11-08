using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services
{
    public sealed class SearchService : ISearchService
    {
        private readonly ILibraryService _libraryService;

        public SearchService(ILibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        public SearchResult SearchLocalLibrary(string query)
        {
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();
            IReadOnlyList<MediaViewModel> videosLibrary = _libraryService.GetVideosFetchResult();

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

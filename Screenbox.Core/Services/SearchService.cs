using System;
using System.Collections.Immutable;
using System.Linq;
using Screenbox.Core.Contexts;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services;

public sealed class SearchService : ISearchService
{
    public SearchResult SearchLocalLibrary(LibraryContext context, string query)
    {
        ImmutableList<MediaViewModel> songs = context.MusicLibrary.Songs
            .Select(m => (Song: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Song)
            .ToImmutableList();
        ImmutableList<AlbumViewModel> albums = context.MusicLibrary.Albums
            .Select(pair => (Album: pair.Value, Index: pair.Key.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Album)
            .ToImmutableList();
        ImmutableList<ArtistViewModel> artists = context.MusicLibrary.Artists
            .Select(pair => (Artist: pair.Value, Index: pair.Key.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Artist)
            .ToImmutableList();
        ImmutableList<MediaViewModel> videos = context.VideosLibrary.Videos
            .Select(m => (Video: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Video)
            .ToImmutableList();

        return new SearchResult(query, songs, videos, artists, albums);
    }
}

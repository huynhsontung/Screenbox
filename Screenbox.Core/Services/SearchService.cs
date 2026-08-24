using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Services;

public sealed class SearchService : ISearchService
{
    private const int MaxSuggestionsPerCategory = 6;
    private const int MaxTotalSuggestions = 10;
    private const double IndexWeightFactor = 0.1;

    private readonly LibraryContext _libraryContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    /// <param name="libraryContext">The library context to search within.</param>
    public SearchService(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
    }

    public SearchResult SearchLocalLibrary(string query)
    {
        ImmutableList<MediaViewModel> songs = _libraryContext.Music.Songs
            .Select(m => (Song: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Song)
            .ToImmutableList();
        ImmutableList<AlbumViewModel> albums = _libraryContext.Music.Albums
            .Select(pair => (Album: pair.Value, Index: pair.Value.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Album)
            .ToImmutableList();
        ImmutableList<ArtistViewModel> artists = _libraryContext.Music.Artists
            .Select(pair => (Artist: pair.Value, Index: pair.Value.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Artist)
            .ToImmutableList();
        ImmutableList<MediaViewModel> videos = _libraryContext.Videos.Videos
            .Select(m => (Video: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
            .Where(t => t.Index >= 0)
            .OrderBy(t => t.Index)
            .Select(t => t.Video)
            .ToImmutableList();

        return new SearchResult(query, songs, videos, artists, albums);
    }

    public void PopulateSuggestions(IList<SearchSuggestion> suggestions, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            suggestions.Clear();
            return;
        }

        string queryText = text.Trim();
        var result = SearchLocalLibrary(queryText);
        var newSuggestions = BuildSuggestions(result, queryText).ToList();

        if (newSuggestions.Count == 0)
        {
            newSuggestions.Add(new SearchSuggestion(SearchSuggestionType.None, queryText));
        }

        for (int i = 0; i < newSuggestions.Count; i++)
        {
            if (i < suggestions.Count)
            {
                var newItem = newSuggestions[i];
                var oldItem = suggestions[i];
                if (!Equals(oldItem, newItem))
                {
                    suggestions[i] = newItem;
                }
            }
            else
            {
                suggestions.Add(newSuggestions[i]);
            }
        }

        for (int i = suggestions.Count - 1; i >= newSuggestions.Count; i--)
        {
            suggestions.RemoveAt(i);
        }
    }

    private static IReadOnlyList<SearchSuggestion> BuildSuggestions(SearchResult result, string searchQuery)
    {
        if (!result.HasItems) return Array.Empty<SearchSuggestion>();

        IEnumerable<SearchSuggestion> songs = result.Songs
            .Take(MaxSuggestionsPerCategory)
            .Select(s => new SearchSuggestion(SearchSuggestionType.Song, s.Name, s));
        IEnumerable<SearchSuggestion> videos = result.Videos
            .Take(MaxSuggestionsPerCategory)
            .Select(v => new SearchSuggestion(SearchSuggestionType.Video, v.Name, v));
        IEnumerable<SearchSuggestion> artists = result.Artists
            .Take(MaxSuggestionsPerCategory)
            .Select(a => new SearchSuggestion(SearchSuggestionType.Artist, a.Name, a));
        IEnumerable<SearchSuggestion> albums = result.Albums
            .Take(MaxSuggestionsPerCategory)
            .Select(a => new SearchSuggestion(SearchSuggestionType.Album, a.Name, a));
        IEnumerable<(double, SearchSuggestion)> searchResults = songs
            .Concat(videos).Concat(artists).Concat(albums)
            .Select(item => (GetRanking(item.Text, searchQuery), item))
            .OrderBy(t => t.Item1)
            .Take(MaxTotalSuggestions);

        return searchResults.Select(t => t.Item2).ToArray();
    }

    private static double GetRanking(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
            return -1;

        int index = text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        if (query.Contains(' ') || index < 0)
            return index;

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double wordRank = words
            .Select(s => s.IndexOf(query, StringComparison.CurrentCultureIgnoreCase))
            .Where(i => i >= 0)
            .Average();

        return (index * IndexWeightFactor) + wordRank;
    }
}

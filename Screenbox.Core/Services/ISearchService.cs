using System.Collections.Generic;
using Screenbox.Core.Contexts;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public interface ISearchService
{
    SearchResult SearchLocalLibrary(LibraryContext context, string query);

    /// <summary>
    /// Populates the provided collection with search suggestions for the given query.
    /// </summary>
    /// <param name="suggestions">The collection to populate with suggestions.</param>
    /// <param name="text">The search query text.</param>
    void PopulateSuggestions(IList<SearchSuggestion> suggestions, string text);
}

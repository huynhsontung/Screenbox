using System.Collections.Generic;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public interface ISearchService
{
    SearchResult SearchLocalLibrary(string query);

    /// <summary>
    /// Gets search suggestions for the specified query text.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <returns>The search suggestions.</returns>
    IReadOnlyList<SearchSuggestion> GetSuggestions(string query);
}

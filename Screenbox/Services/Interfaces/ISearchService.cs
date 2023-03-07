using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface ISearchService
    {
        SearchResult SearchLocalLibrary(string query);
    }
}
using Screenbox.Core.Contexts;
using Screenbox.Core.Models;

namespace Screenbox.Core.Services;

public interface ISearchService
{
    SearchResult SearchLocalLibrary(LibraryContext context, string query);
}

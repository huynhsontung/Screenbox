using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal interface ILibraryService
    {
        Task<IList<MediaViewModel>> FetchSongsAsync(bool ignoreCache = false);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core;
using Screenbox.ViewModels;
using Windows.Foundation;

namespace Screenbox.Services
{
    internal interface ILibraryService
    {
        event TypedEventHandler<ILibraryService, object>? MusicLibraryContentChanged;
        event TypedEventHandler<ILibraryService, object>? VideosLibraryContentChanged;
        Task<MusicLibraryFetchResult> FetchMusicAsync(bool useCache = true);
        Task<IReadOnlyList<MediaViewModel>> FetchVideosAsync(bool useCache = true);
        MusicLibraryFetchResult GetMusicCache();
        IReadOnlyList<MediaViewModel> GetVideosCache();
    }
}
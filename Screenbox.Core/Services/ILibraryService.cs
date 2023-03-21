#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

using Windows.Foundation;
using Screenbox.Core.Models;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services
{
    public interface ILibraryService
    {
        event TypedEventHandler<ILibraryService, object>? MusicLibraryContentChanged;
        event TypedEventHandler<ILibraryService, object>? VideosLibraryContentChanged;
        Task<MusicLibraryFetchResult> FetchMusicAsync(bool useCache = true);
        Task<IReadOnlyList<MediaViewModel>> FetchVideosAsync(bool useCache = true);
        MusicLibraryFetchResult GetMusicCache();
        IReadOnlyList<MediaViewModel> GetVideosCache();
    }
}
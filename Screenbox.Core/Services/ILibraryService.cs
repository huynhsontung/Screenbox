#nullable enable

using Screenbox.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services
{
    public interface ILibraryService
    {
        event TypedEventHandler<ILibraryService, object>? MusicLibraryContentChanged;
        event TypedEventHandler<ILibraryService, object>? VideosLibraryContentChanged;
        StorageLibrary? MusicLibrary { get; }
        StorageLibrary? VideosLibrary { get; }
        public bool IsLoadingVideos { get; }
        public bool IsLoadingMusic { get; }
        Task<StorageLibrary> InitializeMusicLibraryAsync();
        Task<StorageLibrary> InitializeVideosLibraryAsync();
        Task FetchMusicAsync(bool useCache);
        Task FetchVideosAsync();
        MusicLibraryFetchResult GetMusicFetchResult();
        IReadOnlyList<MediaViewModel> GetVideosFetchResult();
    }
}
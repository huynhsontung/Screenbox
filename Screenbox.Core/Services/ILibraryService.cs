#nullable enable

using Screenbox.Core.Contexts;
using Screenbox.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services
{
    /// <summary>
    /// Stateless service for library management operations
    /// </summary>
    public interface ILibraryService
    {
        /// <summary>
        /// Initialize the music library
        /// </summary>
        Task<StorageLibrary> InitializeMusicLibraryAsync(LibraryContext context);

        /// <summary>
        /// Initialize the videos library
        /// </summary>
        Task<StorageLibrary> InitializeVideosLibraryAsync(LibraryContext context);

        /// <summary>
        /// Fetch music from the library
        /// </summary>
        Task FetchMusicAsync(LibraryContext context, bool useCache = true);

        /// <summary>
        /// Fetch videos from the library
        /// </summary>
        Task FetchVideosAsync(LibraryContext context, bool useCache = true);

        /// <summary>
        /// Get the current music fetch result from context
        /// </summary>
        MusicLibraryFetchResult GetMusicFetchResult(LibraryContext context);

        /// <summary>
        /// Get the current videos fetch result from context
        /// </summary>
        IReadOnlyList<MediaViewModel> GetVideosFetchResult(LibraryContext context);

        /// <summary>
        /// Remove media from the library
        /// </summary>
        void RemoveMedia(LibraryContext context, MediaViewModel media);
    }
}
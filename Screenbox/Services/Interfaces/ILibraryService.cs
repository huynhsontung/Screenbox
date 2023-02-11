using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal interface ILibraryService
    {
        IReadOnlyList<MediaViewModel> Songs { get; }
        IReadOnlyList<AlbumViewModel> Albums { get; }
        IReadOnlyList<ArtistViewModel> Artists { get; }
        IReadOnlyList<MediaViewModel> Videos { get; }
        Task<IReadOnlyList<MediaViewModel>> FetchSongsAsync(bool useCache = true);
        Task<IReadOnlyList<MediaViewModel>> FetchVideosAsync(bool useCache = true);
    }
}
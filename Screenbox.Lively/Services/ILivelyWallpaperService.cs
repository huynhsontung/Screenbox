using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Lively.Models;
using Windows.Storage;

namespace Screenbox.Lively.Services;

public interface ILivelyWallpaperService
{
    Task<List<LivelyWallpaperModel>> GetAvailableVisualizersAsync();
    Task<LivelyWallpaperModel?> InstallVisualizerAsync(StorageFile wallpaperFile);
}

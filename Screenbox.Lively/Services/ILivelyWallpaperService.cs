#nullable enable

using Screenbox.Lively.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Lively.Services;
public interface ILivelyWallpaperService
{
    Task<List<LivelyWallpaperModel>> GetAvailableVisualizersAsync();
    Task<LivelyWallpaperModel?> InstallVisualizerAsync(StorageFile wallpaperFile);
}

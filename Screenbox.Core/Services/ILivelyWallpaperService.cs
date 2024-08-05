#nullable enable

using Screenbox.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Core.Services;
public interface ILivelyWallpaperService
{
    Task<List<LivelyWallpaperModel>> GetAvailableVisualizersAsync();
    Task<LivelyWallpaperModel?> InstallVisualizerAsync(StorageFile wallpaperFile);
}

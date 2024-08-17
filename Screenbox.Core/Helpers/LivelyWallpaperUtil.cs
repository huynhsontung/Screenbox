using Newtonsoft.Json;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Core.Helpers;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public static class LivelyWallpaperUtil
{
    public static async Task InstallWallpaper(StorageFile wallpaperFile, StorageFolder destinationFolder, bool verify = false)
    {
        if (verify && !await IsWallpaperFile(wallpaperFile))
            return;

        using var zipStream = await wallpaperFile.OpenStreamForReadAsync();
        using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        zipArchive.ExtractToDirectory(destinationFolder.Path);
    }

    public static async Task<bool> IsWallpaperFile(StorageFile wallpaperFile)
    {
        try
        {
            using var zipStream = await wallpaperFile.OpenStreamForReadAsync();
            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var livelyInfoEntry = zipArchive.GetEntry("LivelyInfo.json");
            if (livelyInfoEntry != null)
                return true;
        }
        catch { /* Ignore */ }
        return false;
    }

    public static bool IsLocalWebWallpaper(this LivelyInfoModel model)
    {
        return model.Type == LivelyWallpaperType.web || model.Type == LivelyWallpaperType.webaudio;
    }

    public static bool IsAudioWallpaper(this LivelyInfoModel model)
    {
        // Backward compatibility with old wallpaper file.
        if (model.Type == LivelyWallpaperType.webaudio)
            return true;

        return IsWallpaperArgPresent(model, "--audio");
    }

    public static bool IsMusicWallpaper(this LivelyInfoModel model)
    {
        return IsWallpaperArgPresent(model, "--system-nowplaying");
    }

    public static bool IsPauseNotify(this LivelyInfoModel model)
    {
        return IsWallpaperArgPresent(model, "--pause-event true");
    }

    private static bool IsWallpaperArgPresent(LivelyInfoModel model, string arg)
    {
        return !string.IsNullOrWhiteSpace(model.Arguments) && model.Arguments.Contains(arg);
    }
}

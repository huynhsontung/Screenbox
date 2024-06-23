using Newtonsoft.Json;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using System;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Core.Factories;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public sealed class LivelyWallpaperFactory
{
    public async Task<(LivelyWallpaperModel, bool)> TryGetWallpaper(StorageFolder wallpaperFolder, bool isPreset)
    {
        var (model, success) = await TryGetWallpaperMetadata(wallpaperFolder);
        if (success)
        {
            var obj = new LivelyWallpaperModel
            {
                Model = model,
                Path = wallpaperFolder.Path,
                IsAudio = LivelyWallpaperUtil.IsAudioWallpaper(model),
                IsMusic = LivelyWallpaperUtil.IsMusicWallpaper(model),
                IsPauseNotify = LivelyWallpaperUtil.IsPauseNotify(model),
                IsPreset = isPreset,
                // Guaranteed to have minimum thumbnail (if created using Lively.)
                PreviewPath = (await wallpaperFolder.GetFileAsync(model.Preview ?? model.Thumbnail)).Path,
            };
            if (TrySanitizeUrl(model.Contact, out Uri uri))
                obj.AuthorUrl = uri;

            return (obj, true);
        }
        return (null, false);
    }

    public async Task<(LivelyInfoModel, bool)> TryGetWallpaperMetadata(StorageFile wallpaperFile)
    {
        try
        {
            using var zipStream = await wallpaperFile.OpenStreamForReadAsync();
            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var livelyInfoEntry = zipArchive.GetEntry("LivelyInfo.json");
            if (livelyInfoEntry == null)
                return (null, false);

            using var entryStream = livelyInfoEntry.Open();
            using var streamReader = new StreamReader(entryStream);
            var jsonContent = await streamReader.ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<LivelyInfoModel>(jsonContent);
            return (model, true);
        }
        catch
        {
            return (null, false);
        }
    }

    public async Task<(LivelyInfoModel, bool)> TryGetWallpaperMetadata(StorageFolder wallpaperFolder)
    {
        try
        {
            var modelFile = await wallpaperFolder.GetFileAsync("LivelyInfo.json");
            var jsonContent = await FileIO.ReadTextAsync(modelFile);
            var model = JsonConvert.DeserializeObject<LivelyInfoModel>(jsonContent);
            return (model, true);
        }
        catch
        {
            return (null, false);
        }
    }

    private static bool TrySanitizeUrl(string address, out Uri uri)
    {
        uri = null;
        try
        {
            try
            {
                uri = new Uri(address);
            }
            catch (UriFormatException)
            {
                //if not specified https/http assume https connection.
                uri = new UriBuilder(address)
                {
                    Scheme = "https",
                    Port = -1,
                }.Uri;
            }
        }
        catch
        {
            return false;
        }
        return true;
    }
}

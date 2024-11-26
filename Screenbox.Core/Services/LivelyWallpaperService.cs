#nullable enable

using Newtonsoft.Json;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Core.Services;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public class LivelyWallpaperService : ILivelyWallpaperService
{
    public async Task<List<LivelyWallpaperModel>> GetAvailableVisualizersAsync()
    {
        var localFolder = ApplicationData.Current.LocalFolder;
        var installFolder = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
        var defaultVisualizerFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(installFolder, "Assets", "Visualizers"));
        var userVisualizerFolder = await localFolder.CreateFolderAsync("Visualizers", CreationCollisionOption.OpenIfExists);
        var defaultVisualizers = await defaultVisualizerFolder.GetFoldersAsync();
        var userVisualizers = await userVisualizerFolder.GetFoldersAsync();
        var allVisualizers = defaultVisualizers.Select(folder => (folder, isPreset: true))
            .Concat(userVisualizers.Select(folder => (folder, isPreset: false)));

        var results = await Task.WhenAll(allVisualizers.Select(tuple => TryGetWallpaper(tuple.folder, tuple.isPreset)));
        return results.OfType<LivelyWallpaperModel>().ToList();
    }

    public async Task<LivelyWallpaperModel?> InstallVisualizerAsync(StorageFile wallpaperFile)
    {
        using var zipStream = await wallpaperFile.OpenStreamForReadAsync();
        using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var result = await TryGetWallpaperMetadata(zipArchive);
        if (result is { } model)
        {
            // We are skipping wallpapers without albumart.
            // Optionally can allow this and just display the albumart in-app instead.
            if (!model.IsLocalWebWallpaper() || !model.IsMusicWallpaper())
            {
                // Optional: Show relevant error message.
                return null;
            }

            var localFolder = ApplicationData.Current.LocalFolder;
            var visualizersFolder =
                await localFolder.CreateFolderAsync("Visualizers", CreationCollisionOption.OpenIfExists);
            var destinationFolder =
                await visualizersFolder.CreateFolderAsync(Path.GetRandomFileName(),
                    CreationCollisionOption.FailIfExists);

            zipArchive.ExtractToDirectory(destinationFolder.Path);
            var wallpaperModel = await TryGetWallpaper(destinationFolder, false);
            if (wallpaperModel == null)
            {
                await destinationFolder.DeleteAsync();
                return null;
            }

            return wallpaperModel;
        }
        else
        {
            // Optional: Show error message.
        }
        return null;
    }

    public async Task<LivelyWallpaperModel?> TryGetWallpaper(StorageFolder wallpaperFolder, bool isPreset)
    {
        var result = await TryGetWallpaperMetadata(wallpaperFolder);
        if (result is { } model)
        {
            var obj = new LivelyWallpaperModel
            {
                Model = model,
                Path = wallpaperFolder.Path,
                IsAudio = model.IsAudioWallpaper(),
                IsMusic = model.IsMusicWallpaper(),
                IsPauseNotify = model.IsPauseNotify(),
                IsPreset = isPreset,
                // Guaranteed to have minimum thumbnail (if created using Lively.)
                PreviewPath = (await wallpaperFolder.GetFileAsync(model.Preview ?? model.Thumbnail)).Path,
            };
            if (TrySanitizeUrl(model.Contact, out Uri uri))
                obj.AuthorUrl = uri;

            return obj;
        }

        return null;
    }

    private async Task<LivelyInfoModel?> TryGetWallpaperMetadata(ZipArchive wallpaperFile)
    {
        try
        {
            var livelyInfoEntry = wallpaperFile.GetEntry("LivelyInfo.json");
            if (livelyInfoEntry == null)
                return null;

            using var entryStream = livelyInfoEntry.Open();
            using var streamReader = new StreamReader(entryStream);
            var jsonContent = await streamReader.ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<LivelyInfoModel>(jsonContent);
            return model;
        }
        catch
        {
            return null;
        }
    }

    private async Task<LivelyInfoModel?> TryGetWallpaperMetadata(StorageFolder wallpaperFolder)
    {
        try
        {
            var modelFile = await wallpaperFolder.GetFileAsync("LivelyInfo.json");
            var jsonContent = await FileIO.ReadTextAsync(modelFile);
            var model = JsonConvert.DeserializeObject<LivelyInfoModel>(jsonContent);
            return model;
        }
        catch
        {
            return null;
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Playback;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.Services;

public sealed class FilesService : IFilesService
{
    private readonly ILogger<FilesService> _logger;

    public FilesService(ILogger<FilesService> logger)
    {
        _logger = logger;
    }

    public async Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null)
    {
        try
        {
            StorageFolder? parent = await file.GetParentAsync();
            options ??= new QueryOptions(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedFormats);
            StorageFileQueryResult? queryResult = parent?.CreateFileQueryWithOptions(options);
            return queryResult;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery)
    {
        // Due to limitations with NeighboringFilesQuery, manually find the next supported file
        uint startIndex = await neighboringFilesQuery.FindStartIndexAsync(currentFile);
        if (startIndex == uint.MaxValue) return null;
        startIndex += 1;

        // The following line return a native vector view.
        // It does not fetch all the files in the directory at once.
        // No need for manual paging!
        IReadOnlyList<StorageFile> files = await neighboringFilesQuery.GetFilesAsync(startIndex, uint.MaxValue);
        return files.FirstOrDefault(x => x.IsSupported());
    }

    public async Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery)
    {
        // Due to limitations with NeighboringFilesQuery, manually find the previous supported file
        uint startIndex = await neighboringFilesQuery.FindStartIndexAsync(currentFile);
        if (startIndex == uint.MaxValue) return null;

        // The following line return a native vector view.
        // It does not fetch all the files in the directory at once.
        // No need for manual paging!
        IReadOnlyList<StorageFile> files = await neighboringFilesQuery.GetFilesAsync(0, startIndex);
        return files.LastOrDefault(x => x.IsSupported());
    }

    public StorageItemQueryResult GetSupportedItems(StorageFolder folder)
    {
        // Don't use indexer when querying. Potential incomplete result.
        QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedFormats);
        return folder.CreateItemQueryWithOptions(queryOptions);
    }

    public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder)
    {
        QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedFormats);
        return folder.CreateItemQueryWithOptions(queryOptions).GetItemCountAsync();
    }

    public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats)
    {
        FileOpenPicker picker = GetFilePickerForFormats(formats);
        return picker.PickSingleFileAsync();
    }

    public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats)
    {
        FileOpenPicker picker = GetFilePickerForFormats(formats);
        return picker.PickMultipleFilesAsync();
    }

    public IAsyncOperation<StorageFile> PickSaveFileAsync(string suggestedFileName,
        IDictionary<string, IList<string>> fileTypes, PickerLocationId startLocation = PickerLocationId.ComputerFolder)
    {
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = startLocation,
            SuggestedFileName = suggestedFileName
        };

        foreach (var fileType in fileTypes)
        {
            picker.FileTypeChoices.Add(fileType.Key, fileType.Value);
        }

        return picker.PickSaveFileAsync();
    }

    public IAsyncOperation<StorageFolder> PickFolderAsync()
    {
        FolderPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        foreach (string supportedFormat in FilesHelpers.SupportedFormats)
        {
            picker.FileTypeFilter.Add(supportedFormat);
        }

        return picker.PickSingleFolderAsync();
    }

    public async Task OpenFileLocationAsync(string path)
    {
        string? folderPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folderPath))
            await Launcher.LaunchFolderPathAsync(folderPath);
    }

    public async Task OpenFileLocationAsync(StorageFile file)
    {
        StorageFolder? folder = await file.GetParentAsync();
        if (folder == null)
        {
            await OpenFileLocationAsync(file.Path);
        }
        else
        {
            FolderLauncherOptions options = new();
            options.ItemsToSelect.Add(file);
            await Launcher.LaunchFolderAsync(folder, options);
        }
    }

    public async Task OpenFolderAsync(StorageFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        await Launcher.LaunchFolderAsync(folder);
    }

    public string? AddToRecent(IStorageItem item)
    {
        string metadata = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        try
        {
            return StorageApplicationPermissions.MostRecentlyUsedList.Add(item, metadata);
        }
        catch (Exception)
        {
            // System.Exception: Element not found. (Exception from HRESULT: 0x80070490)
            // Ownership issue?
            return null;
        }
    }

    public IAsyncOperation<StorageFolder> GetFrameCaptureFolderAsync(string token)
    {
        return GetFrameCaptureFolderFromTokenAsync(token).AsAsyncOperation();
    }

    public string SetFrameCaptureFolder(StorageFolder folder, string? previousToken = null)
    {
        ArgumentNullException.ThrowIfNull(folder);

        // Release the previous entry so the FutureAccessList (capped at 1000 items) doesn't accumulate stale folders.
        if (!string.IsNullOrEmpty(previousToken) && StorageApplicationPermissions.FutureAccessList.ContainsItem(previousToken))
        {
            StorageApplicationPermissions.FutureAccessList.Remove(previousToken);
        }

        return StorageApplicationPermissions.FutureAccessList.Add(folder, "frame-capture");
    }

    public async Task<StorageFile> SaveSnapshotAsync(IMediaPlayer mediaPlayer, string frameCaptureFolderToken)
    {
        ArgumentNullException.ThrowIfNull(mediaPlayer);

        StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
            $"snapshot_{DateTimeOffset.Now.Ticks}",
            CreationCollisionOption.FailIfExists);

        try
        {
            if (!mediaPlayer.TryTakeSnapshot(tempFolder.Path))
            {
                throw new Exception("Media player failed to save snapshot");
            }

            StorageFile file = (await tempFolder.GetFilesAsync())[0];
            StorageFolder destFolder = await GetFrameCaptureFolderAsync(frameCaptureFolderToken);
            return await file.CopyAsync(
                destFolder,
                $"Screenbox_{DateTimeOffset.Now:yyyyMMdd_HHmmss}{file.FileType}",
                NameCollisionOption.GenerateUniqueName);
        }
        finally
        {
            await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }
    }

    public async Task<StorageFile?> TryGetFileFromPathAsync(string path)
    {
        try
        {
            return await StorageFile.GetFileFromPathAsync(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<StorageFolder?> TryGetFolderFromPathAsync(string path)
    {
        try
        {
            return await StorageFolder.GetFolderFromPathAsync(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<StorageFile?> TryGetFileFromTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            if (StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }

            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFileAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }
        }
        catch (Exception)
        {
            // Access cache lookup failed or unavailable.
        }

        return null;
    }

    public async Task<StorageFolder?> TryGetFolderFromTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            if (StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }

            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }
        }
        catch (Exception)
        {
            // Access cache lookup failed or unavailable.
        }

        return null;
    }

    public async Task<StorageFile?> ResolveFileAsync(IFileMediaSource fileSource)
    {
        ArgumentNullException.ThrowIfNull(fileSource);

        if (fileSource.StorageFile is not null)
        {
            return fileSource.StorageFile;
        }

        StorageFile? file = await TryGetFileFromTokenAsync(fileSource.Token);
        if (file is null && !string.IsNullOrEmpty(fileSource.Path))
        {
            file = await TryGetFileFromPathAsync(fileSource.Path);
        }

        if (file is not null)
        {
            fileSource.UpdateStorageFile(file);
        }

        return file;
    }

    private static async Task<StorageFolder> GetFrameCaptureFolderFromTokenAsync(string token)
    {
        if (!string.IsNullOrEmpty(token)
            && StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
        {
            try
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
            }
            catch (Exception e) when (e is FileNotFoundException or UnauthorizedAccessException)
            {
                // Folder was moved, deleted, or access was revoked.
            }
        }

        try
        {
            StorageLibrary pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            StorageFolder savePicturesFolder = pictureLibrary.SaveFolder;

            return await savePicturesFolder.CreateFolderAsync("Screenbox", CreationCollisionOption.OpenIfExists);
        }
        catch (UnauthorizedAccessException)
        {
            // Pictures library permission missing or access denied.
        }

        // Fall back to captures folder.
        return KnownFolders.AppCaptures;
    }

    private FileOpenPicker GetFilePickerForFormats(IReadOnlyCollection<string> formats)
    {
        FileOpenPicker picker = new()
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        IEnumerable<string> fileTypes = formats;
        if (formats.Count == 0)
        {
            fileTypes = FilesHelpers.SupportedFormats;
            picker.FileTypeFilter.Add("*");
        }

        foreach (string? fileType in fileTypes)
        {
            picker.FileTypeFilter.Add(fileType);
        }

        return picker;
    }
}

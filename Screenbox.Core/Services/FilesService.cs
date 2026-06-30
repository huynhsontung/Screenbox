#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.Services;

public sealed class FilesService : IFilesService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

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

    public void AddToRecent(IStorageItem item)
    {
        string metadata = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        try
        {
            StorageApplicationPermissions.MostRecentlyUsedList.Add(item, metadata);
        }
        catch (Exception)
        {
            // System.Exception: Element not found. (Exception from HRESULT: 0x80070490)
            // Ownership issue?
        }
    }

    public async Task<MediaInfo> GetMediaInfoAsync(StorageFile file)
    {
        MediaPlaybackType mediaType = FilesHelpers.GetMediaTypeForFile(file);
        if (!file.IsAvailable) return new MediaInfo(mediaType);

        try
        {
            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            switch (mediaType)
            {
                case MediaPlaybackType.Video:
                    VideoProperties videoProperties = await file.Properties.GetVideoPropertiesAsync();
                    return new MediaInfo(basicProperties, videoProperties);
                case MediaPlaybackType.Music:
                    MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();
                    return new MediaInfo(basicProperties, musicProperties);
            }
        }
        catch (Exception e) when (IsExpectedStoragePropertiesHResult(e.HResult))
        {
            // Expected transient WinRT failures while querying StorageFile properties:
            //   0x800706BA RPC_S_SERVER_UNAVAILABLE - the RPC server is unavailable.
            //   0x8000000E E_ILLEGAL_METHOD_CALL    - file's property provider isn't ready
            //                                         (file became unavailable after IsAvailable,
            //                                          app suspended, or item disposed).
            //   0x80070490 ERROR_NOT_FOUND          - element not found.
            // These are already handled by returning a default MediaInfo; don't report to Sentry.
        }
        catch (Exception e)
        {
            LogService.Log(e);
        }

        return new MediaInfo(mediaType);
    }

    private static bool IsExpectedStoragePropertiesHResult(int hresult)
    {
        const int RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
        const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000E);
        const int ERROR_NOT_FOUND = unchecked((int)0x80070490);
        return hresult == RPC_S_SERVER_UNAVAILABLE
               || hresult == E_ILLEGAL_METHOD_CALL
               || hresult == ERROR_NOT_FOUND;
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

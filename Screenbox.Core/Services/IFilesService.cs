#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

public interface IFilesService
{
    public Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null);
    public Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile,
        StorageFileQueryResult neighboringFilesQuery);
    public Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile,
        StorageFileQueryResult neighboringFilesQuery);
    public StorageItemQueryResult GetSupportedItems(StorageFolder folder);
    public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder);
    public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
    public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats);
    public IAsyncOperation<StorageFile> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypes, PickerLocationId startLocation = PickerLocationId.ComputerFolder);
    public IAsyncOperation<StorageFolder> PickFolderAsync();
    public Task OpenFileLocationAsync(string path);
    public Task OpenFileLocationAsync(StorageFile file);
    public void AddToRecent(IStorageItem item);
    public Task<MediaInfo> GetMediaInfoAsync(StorageFile file);

    /// <summary>
    /// Retrieves the frame captures <see cref="StorageFolder"/> for the given token.
    /// </summary>
    /// <param name="token">The token of the frame captures folder.</param>
    /// <returns>
    /// When this method completes successfully, it returns the frame captures folder
    /// that is associated with the specified token.
    /// </returns>
    IAsyncOperation<StorageFolder> GetFrameCaptureFolderAsync(string token);
}

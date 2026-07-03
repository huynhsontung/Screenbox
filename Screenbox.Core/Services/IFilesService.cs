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
}

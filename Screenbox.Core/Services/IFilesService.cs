#nullable enable

using Screenbox.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services
{
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
        public IAsyncOperation<StorageFolder> PickFolderAsync();
        public Task OpenFileLocationAsync(string path);
        public Task OpenFileLocationAsync(StorageFile file);
        public void AddToRecent(IStorageItem item);
        public Task<StorageFile> SaveToDiskAsync<T>(StorageFolder folder, string fileName, T source);
        public Task SaveToDiskAsync<T>(StorageFile file, T source);
        public Task<T> LoadFromDiskAsync<T>(StorageFolder folder, string fileName);
        public Task<T> LoadFromDiskAsync<T>(StorageFile file);
        public Task<MediaInfo> GetMediaInfoAsync(StorageFile file);
    }
}
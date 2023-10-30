#nullable enable

using Screenbox.Core.Playback;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
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
        public Task<StorageItemThumbnail?> GetThumbnailAsync(StorageFile file, bool allowIcon = false);
        public StorageItemQueryResult GetSupportedItems(StorageFolder folder);
        public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder);
        public StorageFileQueryResult GetSongsFromLibrary();
        public StorageFileQueryResult GetVideosFromLibrary();
        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
        public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats);
        public IAsyncOperation<StorageFolder> PickFolderAsync();
        public Task<StorageFile> SaveSnapshotAsync(IMediaPlayer mediaPlayer);
        public Task OpenFileLocationAsync(StorageFile file);
        public void AddToRecent(IStorageItem item);
    }
}
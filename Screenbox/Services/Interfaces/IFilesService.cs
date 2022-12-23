#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;
using Screenbox.Core.Playback;

namespace Screenbox.Services
{
    internal interface IFilesService
    {
        public Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file);
        public Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);
        public Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);
        public Task<BitmapImage?> GetThumbnailAsync(StorageFile file, bool allowIcon = false);
        public StorageItemQueryResult GetSupportedItems(StorageFolder folder);
        public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder);
        public StorageFileQueryResult GetSongsFromLibrary();
        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
        public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats);
        public IAsyncOperation<StorageFolder> PickFolderAsync();
        public Task<StorageFile> SaveSnapshotAsync(IMediaPlayer mediaPlayer);
        public Task OpenFileLocationAsync(StorageFile file);
    }
}
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
        public Task<BitmapImage?> GetThumbnailAsync(StorageFile file);
        public IAsyncOperation<IReadOnlyList<StorageFile>> GetSupportedFilesAsync(StorageFolder folder);
        public IAsyncOperation<IReadOnlyList<StorageFile>> GetSongsFromLibraryAsync(uint startIndex, uint maxNumberOfItems = 50);
        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
        public Task<StorageFile> SaveSnapshotAsync(IMediaPlayer mediaPlayer);
        public Task OpenFileLocationAsync(StorageFile file);
    }
}
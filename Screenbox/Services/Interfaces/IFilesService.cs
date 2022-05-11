#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;
using LibVLCSharp.Shared;
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal interface IFilesService
    {
        public Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file);
        public Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);

        public Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);
        public Task<List<MediaViewModel>> LoadVideosFromLibraryAsync();
        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
        public Task<StorageFile> SaveSnapshot(MediaPlayer mediaPlayer);
    }
}
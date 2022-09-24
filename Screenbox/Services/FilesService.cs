#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;
using Screenbox.Core.Playback;

namespace Screenbox.Services
{
    internal class FilesService : IFilesService
    {
        private ImmutableArray<string> SupportedFormats { get; } = ImmutableArray.Create(
            // Video formats
            ".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv", ".3gp", ".3g2", ".m4v", ".mpg", ".mpeg", ".webm",
            // Audio formats
            ".mp3", ".wav", ".wma", ".aac", ".mid", ".midi", ".mpa", ".ogg", ".oga", ".weba");

        private ImmutableArray<string> SupportedAudioFormats { get; } = ImmutableArray.Create(
            ".mp3", ".wav", ".wma", ".aac", ".mid", ".midi", ".mpa", ".ogg", ".oga", ".weba");

        public async Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file)
        {
            try
            {
                StorageFolder? parent = await file.GetParentAsync();
                StorageFileQueryResult? queryResult =
                    parent?.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.DefaultQuery, SupportedFormats));
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
            return files.FirstOrDefault(x => SupportedFormats.Contains(x.FileType.ToLowerInvariant()));
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
            return files.LastOrDefault(x => SupportedFormats.Contains(x.FileType.ToLowerInvariant()));
        }

        public async Task<BitmapImage?> GetThumbnailAsync(StorageFile file)
        {
            if (!file.IsAvailable) return null;
            try
            {
                StorageItemThumbnail? source = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                if (source != null && source.Type == ThumbnailType.Image)
                {
                    BitmapImage thumbnail = new();
                    await thumbnail.SetSourceAsync(source);
                    return thumbnail;
                }
            }
            catch (Exception)
            {
                // pass
            }

            return null;
        }

        public IAsyncOperation<IReadOnlyList<StorageFile>> GetSupportedFilesAsync(StorageFolder folder)
        {
            // Don't use indexer when querying. Potential incomplete result.
            QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, SupportedFormats);
            return folder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync();
        }

        public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder)
        {
            QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, SupportedFormats);
            return folder.CreateItemQueryWithOptions(queryOptions).GetItemCountAsync();
        }

        public StorageFileQueryResult GetSongsFromLibrary()
        {
            string[] customPropertyKeys =
            {
                SystemProperties.Title,
                SystemProperties.Music.Artist,
                SystemProperties.Media.Duration
            };

            QueryOptions queryOptions = new(CommonFileQuery.OrderByTitle, SupportedAudioFormats);
            queryOptions.SetPropertyPrefetch(
                PropertyPrefetchOptions.BasicProperties | PropertyPrefetchOptions.MusicProperties,
                customPropertyKeys);
            return KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOptions);
        }

        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats)
        {
            FileOpenPicker picker = new()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            IEnumerable<string> fileTypes = formats.Length == 0 ? SupportedFormats : formats;
            foreach (string? fileType in fileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            return picker.PickSingleFileAsync();
        }

        public async Task<StorageFile> SaveSnapshotAsync(IMediaPlayer mediaPlayer)
        {
            if (mediaPlayer is not VlcMediaPlayer player)
            {
                throw new NotImplementedException("Not supported on non VLC players");
            }

            StorageFolder? tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
                    $"snapshot_{DateTimeOffset.Now.Ticks}",
                    CreationCollisionOption.FailIfExists);

            try
            {
                if (player.VlcPlayer.TakeSnapshot(0, tempFolder.Path, 0, 0))
                {
                    StorageFile? file = (await tempFolder.GetFilesAsync()).First();
                    StorageLibrary? pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                    StorageFolder? defaultSaveFolder = pictureLibrary.SaveFolder;
                    StorageFolder? destFolder =
                        await defaultSaveFolder.CreateFolderAsync("Screenbox",
                            CreationCollisionOption.OpenIfExists);
                    return await file.CopyAsync(destFolder);
                }

                throw new Exception("VLC failed to save snapshot");
            }
            finally
            {
                await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        public async Task OpenFileLocationAsync(StorageFile file)
        {
            StorageFolder? folder = await file.GetParentAsync();
            if (folder == null)
            {
                string? folderPath = Path.GetDirectoryName(file.Path);
                if (!string.IsNullOrEmpty(folderPath))
                    await Launcher.LaunchFolderPathAsync(folderPath);
            }
            else
            {
                FolderLauncherOptions options = new();
                options.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderAsync(folder, options);
            }
        }
    }
}

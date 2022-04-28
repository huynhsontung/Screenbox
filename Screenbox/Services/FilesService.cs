#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;
using LibVLCSharp.Shared;
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal class FilesService : IFilesService
    {
        public ImmutableArray<string> SupportedFormats { get; } = ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv");

        // TODO: Service should not return a ViewModel
        public async Task<List<MediaViewModel>> LoadVideosFromLibraryAsync()
        {
            var videos = new List<MediaViewModel>();
            var library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            foreach (StorageFolder folder in library.Folders)
            {
                // TODO: Handle folders
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, SupportedFormats);

                IReadOnlyList<StorageFile> files = await folder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync();
                foreach (StorageFile file in files)
                {
                    var itemThumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView);
                    BitmapImage? image = null;

                    if (itemThumbnail != null)
                    {
                        image = new BitmapImage();
                        await image.SetSourceAsync(itemThumbnail);
                    }

                    var videoProperties = await file.Properties.GetVideoPropertiesAsync();

                    videos.Add(new MediaViewModel(file)
                    {
                        Duration = videoProperties?.Duration ?? default,
                        Thumbnail = image
                    });
                }
            }

            return videos;
        }

        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            IEnumerable<string> fileTypes = formats.Length == 0 ? SupportedFormats : formats;
            foreach (var fileType in fileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            return picker.PickSingleFileAsync();
        }

        public async Task<StorageFile> SaveSnapshot(MediaPlayer mediaPlayer)
        {
            var tempFolder =
                await ApplicationData.Current.TemporaryFolder.CreateFolderAsync($"snapshot_{DateTimeOffset.Now.Ticks}",
                    CreationCollisionOption.FailIfExists);

            try
            {
                if (mediaPlayer.TakeSnapshot(0, tempFolder.Path, 0, 0))
                {
                    var file = (await tempFolder.GetFilesAsync()).First();
                    var pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                    var defaultSaveFolder = pictureLibrary.SaveFolder;
                    var destFolder =
                        await defaultSaveFolder.CreateFolderAsync("Screenbox", CreationCollisionOption.OpenIfExists);
                    return await file.CopyAsync(destFolder);
                }

                throw new Exception("VLC failed to save snapshot");
            }
            finally
            {
                await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }
    }
}

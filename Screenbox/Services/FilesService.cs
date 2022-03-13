using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using LibVLCSharp.Shared;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal class FilesService : IFilesService
    {
        public ImmutableArray<string> SupportedFormats { get; } = ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv");

        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary
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

using System.Collections.Generic;
using System.Collections.Immutable;
using Windows.Foundation;
using Windows.Storage;

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
    }
}

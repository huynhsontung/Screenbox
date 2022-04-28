#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using LibVLCSharp.Shared;
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal interface IFilesService
    {
        public ImmutableArray<string> SupportedFormats { get; }

        public Task<List<MediaViewModel>> LoadVideosFromLibraryAsync();
        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
        public Task<StorageFile> SaveSnapshot(MediaPlayer mediaPlayer);
    }
}
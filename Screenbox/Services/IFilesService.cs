using System.Collections.Immutable;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using LibVLCSharp.Shared;

namespace Screenbox.Services
{
    internal interface IFilesService
    {
        public ImmutableArray<string> SupportedFormats { get; }

        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
        public Task<StorageFile> SaveSnapshot(MediaPlayer mediaPlayer);
    }
}
using System.Collections.Immutable;
using Windows.Foundation;
using Windows.Storage;

namespace Screenbox.Services
{
    internal interface IFilesService
    {
        public ImmutableArray<string> SupportedFormats { get; }

        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
    }
}
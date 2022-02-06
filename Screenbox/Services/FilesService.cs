using System.Collections.Immutable;

namespace Screenbox.Services
{
    internal class FilesService : IFilesService
    {
        public ImmutableArray<string> SupportedFormats => _supportedFormats;

        private readonly ImmutableArray<string> _supportedFormats = ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv");
    }
}

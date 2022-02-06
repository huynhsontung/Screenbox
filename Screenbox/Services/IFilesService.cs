using System.Collections.Immutable;

namespace Screenbox.Services
{
    internal interface IFilesService
    {
        public ImmutableArray<string> SupportedFormats { get; }
    }
}
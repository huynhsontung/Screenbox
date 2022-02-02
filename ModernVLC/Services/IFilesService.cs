using System.Collections.Immutable;

namespace ModernVLC.Services
{
    internal interface IFilesService
    {
        public ImmutableArray<string> SupportedFormats { get; }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernVLC.Services
{
    internal class FilesService : IFilesService
    {
        public ImmutableArray<string> SupportedFormats => _supportedFormats;

        private readonly ImmutableArray<string> _supportedFormats = ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv");
    }
}

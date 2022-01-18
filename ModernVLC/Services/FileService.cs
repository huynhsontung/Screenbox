using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernVLC.Services
{
    internal class FileService
    {
        public static readonly ImmutableArray<string> SupportedFormats = ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv");
    }
}
